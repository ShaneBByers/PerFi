using System.Net.Http.Json;
using PerFi.Blazor.Contracts;
using PerFi.Blazor.Services;

namespace PerFi.Blazor.Auth;

public sealed class AuthService(
    IHttpClientFactory httpClientFactory,
    PerFiAuthenticationStateProvider authStateProvider) : IAuthService
{
    public async Task<bool> IsAuthenticatedAsync()
    {
        var client = httpClientFactory.CreateClient(HttpClientNames.AnonymousApiClient);

        try
        {
            var session = await client.GetFromJsonAsync<SessionResponse>("session");
            if (session?.IsAuthenticated is true)
            {
                await authStateProvider.MarkUserAuthenticatedAsync(session.UserName ?? "PerFi User");
                return true;
            }
        }
        catch
        {
            // Return not authenticated when session check fails.
        }

        await authStateProvider.MarkUserLoggedOutAsync();
        return false;
    }

    public async Task<ApiLoginResult> LoginAsync(string username, string password)
    {
        var client = httpClientFactory.CreateClient(HttpClientNames.AnonymousApiClient);
        var response = await client.PostAsJsonAsync("login", new LoginRequest(username, password));

        if (!response.IsSuccessStatusCode)
        {
            var failure = await ApiErrorParser.FromFailedResponseAsync(response);
            return ApiLoginResult.Failure(failure.ErrorMessage ?? "Login failed.");
        }

        var session = await response.Content.ReadFromJsonAsync<SessionResponse>();
        if (session is null || !session.IsAuthenticated)
            return ApiLoginResult.Failure("Login response did not establish a session.");

        SessionResponse? persistedSession;
        try
        {
            persistedSession = await client.GetFromJsonAsync<SessionResponse>("session");
        }
        catch
        {
            persistedSession = null;
        }

        if (persistedSession?.IsAuthenticated is not true)
        {
            await authStateProvider.MarkUserLoggedOutAsync();
            return ApiLoginResult.Failure("Login succeeded, but the browser did not persist the session cookie. Cross-site cookies may be blocked for this site.");
        }

        await authStateProvider.MarkUserAuthenticatedAsync(persistedSession.UserName ?? username);
        return ApiLoginResult.Success();
    }

    public async Task LogoutAsync()
    {
        var client = httpClientFactory.CreateClient(HttpClientNames.AnonymousApiClient);
        await client.PostAsync("logout", content: null);
        await authStateProvider.MarkUserLoggedOutAsync();
    }

    private sealed record SessionResponse(bool IsAuthenticated, string? UserName);
}
