using System.Net;
using Microsoft.AspNetCore.Components;

namespace PerFi.Blazor.Auth;

public sealed class AuthMessageHandler(
    NavigationManager navigationManager,
    PerFiAuthenticationStateProvider authStateProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // A single page load can fire several concurrent API calls; once one of them has already
            // logged the user out and redirected, skip repeating that for the rest to avoid a redundant
            // navigation/re-render storm on top of whatever per-call error toast the caller shows.
            if (!authStateProvider.IsAuthenticated)
                return response;

            await authStateProvider.MarkUserLoggedOutAsync();

            var relativePath = navigationManager.ToBaseRelativePath(navigationManager.Uri);
            if (relativePath.StartsWith("login", StringComparison.OrdinalIgnoreCase))
                return response;

            var returnUrl = string.IsNullOrWhiteSpace(relativePath)
                ? "/"
                : $"/{relativePath}";

            navigationManager.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: false);
        }

        return response;
    }
}
