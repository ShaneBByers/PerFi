using System.Net;
using Microsoft.AspNetCore.Components;

namespace PerFi.Blazor.Auth;

public sealed class AuthMessageHandler(
    NavigationManager navigationManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            navigationManager.NavigateTo("/login", forceLoad: false);

        return response;
    }
}
