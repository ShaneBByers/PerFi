using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "PerFiBlazorFrontend";
const string AccessTokenClaimType = "perfi:api_token";

builder.Services.AddAuthentication(options =>
{
	options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
	options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
	.AddCookie(options =>
	{
		options.Cookie.Name = "PerFi.Blazor.BFF.Auth";
		options.Cookie.HttpOnly = true;
		// UI and BFF live on different Azure hostnames, so the cookie must be sent cross-site.
		options.Cookie.SameSite = SameSiteMode.None;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
		options.Cookie.IsEssential = true;
		options.SlidingExpiration = true;
		options.ExpireTimeSpan = TimeSpan.FromHours(8);
		// This is a JSON API for the Blazor client, not a page app; return status codes instead of redirecting.
		options.Events = new CookieAuthenticationEvents
		{
			OnRedirectToLogin = context =>
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				return Task.CompletedTask;
			},
			OnRedirectToAccessDenied = context =>
			{
				context.Response.StatusCode = StatusCodes.Status403Forbidden;
				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddHttpClient("PerFiApi", client =>
{
	var apiBaseUrl = ResolvePerFiApiBaseUrl(builder.Configuration, builder.Environment);
	client.BaseAddress = new Uri(apiBaseUrl);
	client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
	options.KnownIPNetworks.Clear();
	options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
	options.AddPolicy(FrontendCorsPolicy, policy =>
	{
		var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

		if (allowedOrigins.Length > 0)
		{
			policy.WithOrigins(allowedOrigins)
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials();
		}
	});
});

// Required for UseAuthorization(); minimal APIs don't register this implicitly like AddControllers() does.
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "PerFi.Blazor.BFF" }))
	.AllowAnonymous();

app.MapPost("/login", async (
	LoginRequest request,
	IHttpClientFactory httpClientFactory,
	HttpContext httpContext,
	ILogger<Program> logger,
	CancellationToken cancellationToken) =>
{
	var client = httpClientFactory.CreateClient("PerFiApi");

	try
	{
		using var response = await client.PostAsJsonAsync("api/auth/login", request, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			var failedBody = await response.Content.ReadAsStringAsync(cancellationToken);
			var failedContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
			return Results.Content(failedBody, failedContentType, Encoding.UTF8, (int)response.StatusCode);
		}

		var loginPayload = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
		if (loginPayload is null || string.IsNullOrWhiteSpace(loginPayload.Token))
			return Results.Problem("The upstream login response did not include a token.", statusCode: StatusCodes.Status502BadGateway);

		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, request.Username),
			new(AccessTokenClaimType, loginPayload.Token)
		};

		var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
		var principal = new ClaimsPrincipal(identity);
		await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

		return Results.Ok(new SessionResponse(true, request.Username));
	}
	catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
	{
		logger.LogError(ex, "Login request to upstream API timed out for user '{UserName}'.", request.Username);
		return Results.Problem(
			"The authentication service is not responding. Please try again in a moment.",
			statusCode: StatusCodes.Status504GatewayTimeout);
	}
	catch (HttpRequestException ex)
	{
		logger.LogError(ex, "Login request to upstream API failed for user '{UserName}'.", request.Username);
		return Results.Problem(
			"The authentication service is currently unavailable. Please try again in a moment.",
			statusCode: StatusCodes.Status503ServiceUnavailable);
	}
}).AllowAnonymous();

app.MapPost("/logout", async (HttpContext httpContext) =>
{
	await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	return Results.Ok(new SessionResponse(false, null));
}).RequireAuthorization();

app.MapGet("/session", (HttpContext httpContext) =>
{
	var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
	var userName = isAuthenticated ? httpContext.User.Identity?.Name : null;
	return Results.Ok(new SessionResponse(isAuthenticated, userName));
}).AllowAnonymous();

app.MapMethods("/{**path}", ["GET", "POST", "PUT", "DELETE", "PATCH"], async (
	HttpContext httpContext,
	IHttpClientFactory httpClientFactory,
	string path,
	CancellationToken cancellationToken) =>
{
	var accessToken = httpContext.User.FindFirst(AccessTokenClaimType)?.Value;
	if (string.IsNullOrWhiteSpace(accessToken))
		return Results.Unauthorized();

	var upstreamPathOnly = NormalizeUpstreamPath(path);
	if (string.IsNullOrWhiteSpace(upstreamPathOnly))
		return Results.NotFound();

	var client = httpClientFactory.CreateClient("PerFiApi");
	var query = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : string.Empty;
	var upstreamPath = $"{upstreamPathOnly}{query}";

	using var proxyRequest = new HttpRequestMessage(new HttpMethod(httpContext.Request.Method), upstreamPath);
	proxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

	if (httpContext.Request.ContentLength is > 0)
	{
		using var bodyReader = new StreamReader(httpContext.Request.Body);
		var body = await bodyReader.ReadToEndAsync(cancellationToken);
		if (!string.IsNullOrWhiteSpace(body))
		{
			var contentType = string.IsNullOrWhiteSpace(httpContext.Request.ContentType)
				? "application/json"
				: httpContext.Request.ContentType;
			var proxyContent = new StringContent(body, Encoding.UTF8);
			proxyContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
			proxyRequest.Content = proxyContent;
		}
	}

	// Catch failures here so the response still flows through the CORS middleware instead of a bare, header-less error.
	try
	{
		using var proxyResponse = await client.SendAsync(proxyRequest, cancellationToken);
		var responseBody = await proxyResponse.Content.ReadAsStringAsync(cancellationToken);
		var responseContentType = proxyResponse.Content.Headers.ContentType?.ToString() ?? "application/json";

		return Results.Content(responseBody, responseContentType, Encoding.UTF8, (int)proxyResponse.StatusCode);
	}
	catch (Exception ex)
	{
		return Results.Problem($"Upstream API call failed: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
	}
}).RequireAuthorization();

app.Run();

static string ResolvePerFiApiBaseUrl(IConfiguration configuration, IWebHostEnvironment environment)
{
	var configuredBaseUrl = configuration["PerFiApi:BaseUrl"];
	if (string.IsNullOrWhiteSpace(configuredBaseUrl))
	{
		if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
			return "http://localhost:5238";

		throw new InvalidOperationException("PerFi API base URL is not configured. Set PerFiApi:BaseUrl in environment configuration.");
	}

	if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out _))
		throw new InvalidOperationException($"Invalid PerFiApi:BaseUrl value '{configuredBaseUrl}'. Configure an absolute URL.");

	return configuredBaseUrl;
}

static string? NormalizeUpstreamPath(string? path)
{
	if (string.IsNullOrWhiteSpace(path))
		return null;

	var trimmedPath = path.TrimStart('/');
	if (trimmedPath.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
		return trimmedPath;

	return $"api/{trimmedPath}";
}

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token);

public sealed record SessionResponse(bool IsAuthenticated, string? UserName);
