using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class AccountContributionPlansApiClient(HttpClient httpClient) : IAccountContributionPlansApiClient
{
    public async Task<IReadOnlyList<AccountContributionPlanResponse>> GetAllByAccountIdAsync(int accountId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<AccountContributionPlanResponse>>($"api/accounts/{accountId}/contribution-plans", cancellationToken) ?? [];

    public async Task<ApiResult> CreateAsync(int accountId, CreateAccountContributionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/accounts/{accountId}/contribution-plans", request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int accountId, int id, UpdateAccountContributionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/accounts/{accountId}/contribution-plans/{id}", request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int accountId, int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/accounts/{accountId}/contribution-plans/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
