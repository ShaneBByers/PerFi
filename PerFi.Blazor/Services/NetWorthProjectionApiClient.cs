using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class NetWorthProjectionApiClient(HttpClient httpClient) : INetWorthProjectionApiClient
{
    public async Task<IReadOnlyList<ProjectionPeriodResponse>> GetProjectionAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<ProjectionPeriodResponse>>("api/networthprojection", cancellationToken) ?? [];
}
