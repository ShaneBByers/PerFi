using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class AccountTypesApiClient(HttpClient httpClient) : IAccountTypesApiClient
{
    public async Task<IReadOnlyList<AccountTypeResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<AccountTypeResponse>>("api/accounttypes", cancellationToken) ?? [];

    public Task<AccountTypeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<AccountTypeResponse>($"api/accounttypes/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/accounttypes", new CreateAccountTypeRequest(name), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
