using System.Net;
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
const string RefreshTokenClaimType = "perfi:api_refresh_token";

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
		// UI (www.per-fi.net) and BFF (auth.per-fi.net) are sibling subdomains of the same registrable domain,
		// so this is a same-site request and Lax is sufficient - avoids Safari ITP blocking SameSite=None cookies.
		options.Cookie.SameSite = SameSiteMode.Lax;
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
		if (loginPayload is null || string.IsNullOrWhiteSpace(loginPayload.Token) || string.IsNullOrWhiteSpace(loginPayload.RefreshToken))
			return Results.Problem("The upstream login response did not include a token.", statusCode: StatusCodes.Status502BadGateway);

		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, request.Username),
			new(AccessTokenClaimType, loginPayload.Token),
			new(RefreshTokenClaimType, loginPayload.RefreshToken)
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

app.MapPost("/logout", async (HttpContext httpContext, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
	var refreshToken = httpContext.User.FindFirst(RefreshTokenClaimType)?.Value;
	if (!string.IsNullOrWhiteSpace(refreshToken))
	{
		// Best-effort: revoke server-side so the refresh token can't be replayed after this cookie is gone.
		try
		{
			var client = httpClientFactory.CreateClient("PerFiApi");
			await client.PostAsJsonAsync("api/auth/revoke", new RefreshRequest(refreshToken), cancellationToken);
		}
		catch (HttpRequestException)
		{
		}
	}

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

	string? requestBody = null;
	string? requestContentType = null;
	if (httpContext.Request.ContentLength is > 0)
	{
		using var bodyReader = new StreamReader(httpContext.Request.Body);
		requestBody = await bodyReader.ReadToEndAsync(cancellationToken);
		requestContentType = string.IsNullOrWhiteSpace(httpContext.Request.ContentType)
			? "application/json"
			: httpContext.Request.ContentType;
	}

	HttpRequestMessage BuildProxyRequest(string bearerToken)
	{
		var request = new HttpRequestMessage(new HttpMethod(httpContext.Request.Method), upstreamPath);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

		if (!string.IsNullOrWhiteSpace(requestBody))
		{
			var proxyContent = new StringContent(requestBody, Encoding.UTF8);
			proxyContent.Headers.ContentType = MediaTypeHeaderValue.Parse(requestContentType!);
			request.Content = proxyContent;
		}

		return request;
	}

	// Catch failures here so the response still flows through the CORS middleware instead of a bare, header-less error.
	try
	{
		using var proxyResponse = await SendAsync(accessToken);

		if (proxyResponse.StatusCode != HttpStatusCode.Unauthorized)
			return await ToResultAsync(proxyResponse);

		// The embedded upstream JWT (60m) is shorter-lived than the BFF cookie (8h sliding) - silently mint a
		// fresh one from the refresh token instead of bouncing the user back to /login on every expiry.
		var refreshedAccessToken = await TryRefreshAccessTokenAsync(httpContext, httpClientFactory, cancellationToken);
		if (refreshedAccessToken is null)
		{
			// Refresh token is also invalid/expired/revoked (or missing) - the session is genuinely over. Signing
			// out here (rather than leaving the stale cookie authenticated) is what stops the client from bouncing
			// forever between the page (401 -> logout+redirect) and /login (session still valid -> redirect back).
			await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return await ToResultAsync(proxyResponse);
		}

		using var retryResponse = await SendAsync(refreshedAccessToken);
		return await ToResultAsync(retryResponse);

		async Task<HttpResponseMessage> SendAsync(string bearerToken)
		{
			using var request = BuildProxyRequest(bearerToken);
			return await client.SendAsync(request, cancellationToken);
		}

		async Task<IResult> ToResultAsync(HttpResponseMessage response)
		{
			var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			var responseContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
			return Results.Content(responseBody, responseContentType, Encoding.UTF8, (int)response.StatusCode);
		}
	}
	catch (Exception ex)
	{
		return Results.Problem($"Upstream API call failed: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
	}
}).RequireAuthorization();

app.Run();

static async Task<string?> TryRefreshAccessTokenAsync(HttpContext httpContext, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken)
{
	var refreshToken = httpContext.User.FindFirst(RefreshTokenClaimType)?.Value;
	var userName = httpContext.User.Identity?.Name;
	if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(userName))
		return null;

	var client = httpClientFactory.CreateClient("PerFiApi");

	try
	{
		using var response = await client.PostAsJsonAsync("api/auth/refresh", new RefreshRequest(refreshToken), cancellationToken);
		if (!response.IsSuccessStatusCode)
			return null;

		var payload = await response.Content.ReadFromJsonAsync<RefreshResponse>(cancellationToken: cancellationToken);
		if (payload is null || string.IsNullOrWhiteSpace(payload.Token) || string.IsNullOrWhiteSpace(payload.RefreshToken))
			return null;

		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, userName),
			new(AccessTokenClaimType, payload.Token),
			new(RefreshTokenClaimType, payload.RefreshToken)
		};

		var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
		await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

		return payload.Token;
	}
	catch (HttpRequestException)
	{
		return null;
	}
}

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

public sealed record LoginResponse(string Token, string RefreshToken);

public sealed record RefreshRequest(string RefreshToken);

public sealed record RefreshResponse(string Token, string RefreshToken);

public sealed record SessionResponse(bool IsAuthenticated, string? UserName);
