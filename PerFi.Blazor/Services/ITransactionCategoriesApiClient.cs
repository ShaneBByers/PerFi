using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface ITransactionCategoriesApiClient
{
    Task<IReadOnlyList<TransactionCategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TransactionCategoryResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(string name, int transactionCategoryGroupId, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, string name, int transactionCategoryGroupId, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedTransactionCategoryIds, CancellationToken cancellationToken = default);
}
