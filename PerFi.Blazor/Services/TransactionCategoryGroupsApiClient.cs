using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class TransactionCategoryGroupsApiClient(HttpClient httpClient) : ITransactionCategoryGroupsApiClient
{
    public async Task<IReadOnlyList<TransactionCategoryGroupResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<TransactionCategoryGroupResponse>>("api/transactioncategorygroups", cancellationToken) ?? [];

    public Task<TransactionCategoryGroupResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<TransactionCategoryGroupResponse>($"api/transactioncategorygroups/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/transactioncategorygroups", new CreateTransactionCategoryGroupRequest(name), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/transactioncategorygroups/{id}", new UpdateTransactionCategoryGroupRequest(name), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/transactioncategorygroups/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedTransactionCategoryGroupIds, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/transactioncategorygroups/reorder", new ReorderTransactionCategoryGroupsRequest(orderedTransactionCategoryGroupIds), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
