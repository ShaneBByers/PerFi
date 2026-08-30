using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class TransactionCategoryGroupService(
    ITransactionCategoryGroupRepository transactionCategoryGroupRepository)
    : ITransactionCategoryGroupService
{
    public async Task<IReadOnlyList<TransactionCategoryGroup>> GetAllTransactionCategoryGroupsAsync(CancellationToken cancellationToken = default)
        => await transactionCategoryGroupRepository.GetAllTransactionCategoryGroupsAsync(cancellationToken);

    public async Task<TransactionCategoryGroup?> GetTransactionCategoryGroupByIdAsync(int id, CancellationToken cancellationToken = default)
        => await transactionCategoryGroupRepository.GetTransactionCategoryGroupByIdAsync(id, cancellationToken);

    public async Task<Result<TransactionCategoryGroup>> CreateTransactionCategoryGroupAsync(CreateTransactionCategoryGroupCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<TransactionCategoryGroup>.Failure("Create transaction category group command cannot be null.");

        try
        {
            var group = new TransactionCategoryGroup(command.Name);
            var result = await transactionCategoryGroupRepository.AddTransactionCategoryGroupAsync(group, cancellationToken);

            if (!result.IsSuccess)
                return Result<TransactionCategoryGroup>.Failure(result.Error);

            group.Id = result.Value;
            return Result<TransactionCategoryGroup>.Success(group);
        }
        catch (ArgumentException ex)
        {
            return Result<TransactionCategoryGroup>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateTransactionCategoryGroupAsync(UpdateTransactionCategoryGroupCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Update transaction category group command cannot be null.");

        try
        {
            var group = new TransactionCategoryGroup(command.TransactionCategoryGroupId, command.Name);
            return await transactionCategoryGroupRepository.UpdateTransactionCategoryGroupAsync(group, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteTransactionCategoryGroupAsync(int transactionCategoryGroupId, CancellationToken cancellationToken = default)
        => await transactionCategoryGroupRepository.DeleteTransactionCategoryGroupAsync(transactionCategoryGroupId, cancellationToken);

    public async Task<Result> ReorderTransactionCategoryGroupsAsync(ReorderTransactionCategoryGroupsCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Reorder transaction category groups command cannot be null.");

        return await transactionCategoryGroupRepository.ReorderTransactionCategoryGroupsAsync(command.OrderedTransactionCategoryGroupIds, cancellationToken);
    }
}