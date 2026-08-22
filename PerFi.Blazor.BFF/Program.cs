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

var cookieDomain = builder.Configuration["Cookie:Domain"];
var sameSiteValue = builder.Configuration["Cookie:SameSite"];
var securePolicyValue = builder.Configuration["Cookie:SecurePolicy"];
var useCrossSiteCookies = string.Equals(builder.Configuration["Cookie:UseCrossSiteCookies"], "true", StringComparison.OrdinalIgnoreCase)
	|| builder.Environment.IsProduction();

if (string.IsNullOrWhiteSpace(sameSiteValue))
	sameSiteValue = useCrossSiteCookies ? nameof(SameSiteMode.None) : nameof(SameSiteMode.Lax);

var cookieSameSite = Enum.TryParse<SameSiteMode>(sameSiteValue, ignoreCase: true, out var parsedSameSite)
	? parsedSameSite
	: (useCrossSiteCookies ? SameSiteMode.None : SameSiteMode.Lax);

var defaultSecurePolicy = builder.Environment.IsDevelopment() && cookieSameSite != SameSiteMode.None
	? CookieSecurePolicy.SameAsRequest
	: CookieSecurePolicy.Always;

var cookieSecurePolicy = Enum.TryParse<CookieSecurePolicy>(securePolicyValue, ignoreCase: true, out var parsedSecurePolicy)
	? parsedSecurePolicy
	: defaultSecurePolicy;

if (cookieSameSite == SameSiteMode.None && cookieSecurePolicy != CookieSecurePolicy.Always)
	cookieSecurePolicy = CookieSecurePolicy.Always;

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.Cookie.Name = "PerFi.Blazor.BFF.Auth";
		options.Cookie.HttpOnly = true;
		options.Cookie.SecurePolicy = cookieSecurePolicy;
		options.Cookie.SameSite = cookieSameSite;
		if (!string.IsNullOrWhiteSpace(cookieDomain))
			options.Cookie.Domain = cookieDomain;
		options.Cookie.IsEssential = true;
		options.SlidingExpiration = true;
		options.ExpireTimeSpan = TimeSpan.FromHours(8);
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

builder.Services.AddAuthorization();

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

var allowedOrigins = ResolveAllowedCorsOrigins(builder.Configuration, builder.Logging);

builder.Services.AddCors(options =>
{
	options.AddPolicy(FrontendCorsPolicy, policy =>
	{
		if (allowedOrigins.Length == 0)
			return;

		policy.WithOrigins(allowedOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});

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

	using var proxyResponse = await client.SendAsync(proxyRequest, cancellationToken);
	var responseBody = await proxyResponse.Content.ReadAsStringAsync(cancellationToken);
	var responseContentType = proxyResponse.Content.Headers.ContentType?.ToString() ?? "application/json";

	return Results.Content(responseBody, responseContentType, Encoding.UTF8, (int)proxyResponse.StatusCode);
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

static string[] ResolveAllowedCorsOrigins(IConfiguration configuration, ILoggingBuilder logging)
{
	var loggerFactory = LoggerFactory.Create(builder =>
	{
		foreach (var provider in logging.Services)
		{
			builder.Services.Add(provider);
		}
	});
	var logger = loggerFactory.CreateLogger("PerFi.Blazor.BFF.Cors");

	var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
	var normalizedOrigins = new List<string>();

	foreach (var configuredOrigin in configuredOrigins)
	{
		if (string.IsNullOrWhiteSpace(configuredOrigin))
			continue;

		var trimmedOrigin = configuredOrigin.Trim().TrimEnd('/');
		if (!Uri.TryCreate(trimmedOrigin, UriKind.Absolute, out var parsedOrigin)
			|| (parsedOrigin.Scheme != Uri.UriSchemeHttp && parsedOrigin.Scheme != Uri.UriSchemeHttps))
		{
			logger.LogWarning("Skipping invalid CORS origin '{Origin}'. Configure absolute http/https origins.", configuredOrigin);
			continue;
		}

		normalizedOrigins.Add($"{parsedOrigin.Scheme}://{parsedOrigin.Authority}");
	}

	var distinctOrigins = normalizedOrigins
		.Distinct(StringComparer.OrdinalIgnoreCase)
		.ToArray();

	if (distinctOrigins.Length == 0)
		logger.LogWarning("No valid CORS origins configured in Cors:AllowedOrigins. Cross-origin browser requests will be blocked.");
	else
		logger.LogInformation("Configured CORS allowed origins: {Origins}", string.Join(", ", distinctOrigins));

	return distinctOrigins;
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
