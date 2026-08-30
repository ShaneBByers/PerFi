using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface ITransactionCategoryGroupService
{
    Task<IReadOnlyList<TransactionCategoryGroup>> GetAllTransactionCategoryGroupsAsync(CancellationToken cancellationToken = default);
    Task<TransactionCategoryGroup?> GetTransactionCategoryGroupByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<TransactionCategoryGroup>> CreateTransactionCategoryGroupAsync(CreateTransactionCategoryGroupCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateTransactionCategoryGroupAsync(UpdateTransactionCategoryGroupCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteTransactionCategoryGroupAsync(int transactionCategoryGroupId, CancellationToken cancellationToken = default);
    Task<Result> ReorderTransactionCategoryGroupsAsync(ReorderTransactionCategoryGroupsCommand command, CancellationToken cancellationToken = default);
}