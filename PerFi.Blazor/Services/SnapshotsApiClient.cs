using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class SnapshotsApiClient(HttpClient httpClient) : ISnapshotsApiClient
{
    public async Task<IReadOnlyList<FinanceSnapshotResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<FinanceSnapshotResponse>>("api/snapshots", cancellationToken) ?? [];

    public Task<FinanceSnapshotResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<FinanceSnapshotResponse>($"api/snapshots/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(DateOnly snapshotDate, IReadOnlyDictionary<int, decimal> accountIdToBalanceMap, CancellationToken cancellationToken = default)
    {
        var request = new CreateFinanceSnapshotRequest(snapshotDate, accountIdToBalanceMap);
        var response = await httpClient.PostAsJsonAsync("api/snapshots", request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
