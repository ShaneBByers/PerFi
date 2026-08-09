using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PerFi.Blazor.Auth;

public sealed class PerFiAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal _currentUser = Anonymous;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(_currentUser));

    public Task MarkUserAuthenticatedAsync(string username)
    {
        _currentUser = CreatePrincipal(username);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        return Task.CompletedTask;
    }

    public Task MarkUserLoggedOutAsync()
    {
        _currentUser = Anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
        return Task.CompletedTask;
    }

    private static ClaimsPrincipal CreatePrincipal(string username)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, username)
        ],
        authenticationType: "bff-cookie");

        return new ClaimsPrincipal(identity);
    }
}
