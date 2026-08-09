using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class AccountTypeGroupsApiClient(HttpClient httpClient) : IAccountTypeGroupsApiClient
{
    public async Task<IReadOnlyList<AccountTypeGroupResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<AccountTypeGroupResponse>>("api/accounttypegroups", cancellationToken) ?? [];

    public Task<AccountTypeGroupResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<AccountTypeGroupResponse>($"api/accounttypegroups/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/accounttypegroups", new CreateAccountTypeGroupRequest(name), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/accounttypegroups/{id}", new UpdateAccountTypeGroupRequest(name), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/accounttypegroups/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}