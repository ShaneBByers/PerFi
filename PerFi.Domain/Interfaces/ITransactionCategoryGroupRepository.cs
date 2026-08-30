using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface ITransactionCategoryGroupRepository
{
    Task<IReadOnlyList<TransactionCategoryGroup>> GetAllTransactionCategoryGroupsAsync(CancellationToken cancellationToken = default);
    Task<TransactionCategoryGroup?> GetTransactionCategoryGroupByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddTransactionCategoryGroupAsync(TransactionCategoryGroup transactionCategoryGroup, CancellationToken cancellationToken = default);
    Task<Result> UpdateTransactionCategoryGroupAsync(TransactionCategoryGroup transactionCategoryGroup, CancellationToken cancellationToken = default);
    Task<Result> DeleteTransactionCategoryGroupAsync(int transactionCategoryGroupId, CancellationToken cancellationToken = default);
    Task<Result> ReorderTransactionCategoryGroupsAsync(IReadOnlyList<int> orderedTransactionCategoryGroupIds, CancellationToken cancellationToken = default);
}
