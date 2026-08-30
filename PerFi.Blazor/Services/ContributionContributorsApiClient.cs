using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class ContributionContributorsApiClient(HttpClient httpClient) : IContributionContributorsApiClient
{
    public async Task<IReadOnlyList<ContributionContributorResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<ContributionContributorResponse>>("api/contributioncontributors", cancellationToken) ?? [];

    public Task<ContributionContributorResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<ContributionContributorResponse>($"api/contributioncontributors/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/contributioncontributors", new CreateContributionContributorRequest(name), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/contributioncontributors/{id}", new UpdateContributionContributorRequest(name), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/contributioncontributors/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedContributionContributorIds, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/contributioncontributors/reorder", new ReorderContributionContributorsRequest(orderedContributionContributorIds), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
