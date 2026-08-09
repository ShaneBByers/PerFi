using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class AccountsApiClient(HttpClient httpClient) : IAccountsApiClient
{
    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<AccountResponse>>("api/accounts", cancellationToken) ?? [];

    public Task<AccountResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<AccountResponse>($"api/accounts/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(string accountName, int institutionId, int accountTypeId, CancellationToken cancellationToken = default)
    {
        var request = new CreateAccountRequest(accountName, institutionId, accountTypeId);
        var response = await httpClient.PostAsJsonAsync("api/accounts", request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, string accountName, int institutionId, int accountTypeId, CancellationToken cancellationToken = default)
    {
        var request = new UpdateAccountRequest(accountName, institutionId, accountTypeId);
        var response = await httpClient.PutAsJsonAsync($"api/accounts/{id}", request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/accounts/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedAccountIds, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/accounts/reorder", new ReorderAccountsRequest(orderedAccountIds), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
