using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface ITransactionCategoryService
{
    Task<IReadOnlyList<TransactionCategory>> GetAllTransactionCategoriesAsync(CancellationToken cancellationToken = default);
    Task<TransactionCategory?> GetTransactionCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<TransactionCategory>> CreateTransactionCategoryAsync(CreateTransactionCategoryCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateTransactionCategoryAsync(UpdateTransactionCategoryCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteTransactionCategoryAsync(int transactionCategoryId, CancellationToken cancellationToken = default);
    Task<Result> ReorderTransactionCategoriesAsync(ReorderTransactionCategoriesCommand command, CancellationToken cancellationToken = default);
}