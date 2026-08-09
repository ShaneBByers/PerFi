using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class AccountTypesApiClient(HttpClient httpClient) : IAccountTypesApiClient
{
    public async Task<IReadOnlyList<AccountTypeResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<AccountTypeResponse>>("api/accounttypes", cancellationToken) ?? [];

    public Task<AccountTypeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<AccountTypeResponse>($"api/accounttypes/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(string name, int accountTypeGroupId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/accounttypes", new CreateAccountTypeRequest(name, accountTypeGroupId), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, string name, int accountTypeGroupId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/accounttypes/{id}", new UpdateAccountTypeRequest(name, accountTypeGroupId), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/accounttypes/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedAccountTypeIds, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/accounttypes/reorder", new ReorderAccountTypesRequest(orderedAccountTypeIds), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
