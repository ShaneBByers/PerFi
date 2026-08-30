using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface ITransactionCategoryGroupsApiClient
{
    Task<IReadOnlyList<TransactionCategoryGroupResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TransactionCategoryGroupResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, string name, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedTransactionCategoryGroupIds, CancellationToken cancellationToken = default);
}
