using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class ContributionsApiClient(HttpClient httpClient) : IContributionsApiClient
{
    public async Task<IReadOnlyList<ContributionResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<ContributionResponse>>("api/contributions", cancellationToken) ?? [];

    public Task<ContributionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<ContributionResponse>($"api/contributions/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(DateOnly date, decimal amount, int contributionContributorId, int accountId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/contributions", new CreateContributionRequest(date, amount, contributionContributorId, accountId), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, DateOnly date, decimal amount, int contributionContributorId, int accountId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/contributions/{id}", new UpdateContributionRequest(date, amount, contributionContributorId, accountId), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/contributions/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
