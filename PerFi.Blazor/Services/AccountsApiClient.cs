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
}
