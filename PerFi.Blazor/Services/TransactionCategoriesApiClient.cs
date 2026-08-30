using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class TransactionCategoriesApiClient(HttpClient httpClient) : ITransactionCategoriesApiClient
{
    public async Task<IReadOnlyList<TransactionCategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<TransactionCategoryResponse>>("api/transactioncategories", cancellationToken) ?? [];

    public Task<TransactionCategoryResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<TransactionCategoryResponse>($"api/transactioncategories/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(string name, int transactionCategoryGroupId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/transactioncategories", new CreateTransactionCategoryRequest(name, transactionCategoryGroupId), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, string name, int transactionCategoryGroupId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/transactioncategories/{id}", new UpdateTransactionCategoryRequest(name, transactionCategoryGroupId), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/transactioncategories/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedTransactionCategoryIds, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/transactioncategories/reorder", new ReorderTransactionCategoriesRequest(orderedTransactionCategoryIds), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
