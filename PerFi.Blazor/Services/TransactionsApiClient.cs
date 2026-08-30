using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class TransactionsApiClient(HttpClient httpClient) : ITransactionsApiClient
{
    public async Task<IReadOnlyList<TransactionResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<TransactionResponse>>("api/transactions", cancellationToken) ?? [];

    public Task<TransactionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<TransactionResponse>($"api/transactions/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(DateOnly date, string counterpartyName, decimal amount, int transactionCategoryId, int accountId, string? description, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/transactions", new CreateTransactionRequest(date, counterpartyName, amount, transactionCategoryId, accountId, description), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, DateOnly date, string counterpartyName, decimal amount, int transactionCategoryId, int accountId, string? description, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/transactions/{id}", new UpdateTransactionRequest(date, counterpartyName, amount, transactionCategoryId, accountId, description), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/transactions/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
