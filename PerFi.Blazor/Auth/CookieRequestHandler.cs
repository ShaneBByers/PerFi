using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace PerFi.Blazor.Auth;

public sealed class CookieRequestHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, cancellationToken);
    }
}
