using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface ITransactionCategoryRepository
{
    Task<IReadOnlyList<TransactionCategory>> GetAllTransactionCategoriesAsync(CancellationToken cancellationToken = default);
    Task<TransactionCategory?> GetTransactionCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddTransactionCategoryAsync(TransactionCategory transactionCategory, int transactionCategoryGroupId, CancellationToken cancellationToken = default);
    Task<Result> UpdateTransactionCategoryAsync(TransactionCategory transactionCategory, int transactionCategoryGroupId, CancellationToken cancellationToken = default);
    Task<Result> DeleteTransactionCategoryAsync(int transactionCategoryId, CancellationToken cancellationToken = default);
    Task<Result> ReorderTransactionCategoriesAsync(IReadOnlyList<int> orderedTransactionCategoryIds, CancellationToken cancellationToken = default);
}