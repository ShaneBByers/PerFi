using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class TransactionCategoryService(
    ITransactionCategoryRepository transactionCategoryRepository,
    ITransactionCategoryGroupRepository transactionCategoryGroupRepository)
    : ITransactionCategoryService
{
    public async Task<IReadOnlyList<TransactionCategory>> GetAllTransactionCategoriesAsync(CancellationToken cancellationToken = default)
        => await transactionCategoryRepository.GetAllTransactionCategoriesAsync(cancellationToken);

    public async Task<TransactionCategory?> GetTransactionCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
        => await transactionCategoryRepository.GetTransactionCategoryByIdAsync(id, cancellationToken);

    public async Task<Result<TransactionCategory>> CreateTransactionCategoryAsync(CreateTransactionCategoryCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<TransactionCategory>.Failure("Create transaction category command cannot be null.");

        var group = await transactionCategoryGroupRepository.GetTransactionCategoryGroupByIdAsync(command.TransactionCategoryGroupId, cancellationToken);
        if (group is null)
            return Result<TransactionCategory>.Failure($"Transaction category group with ID '{command.TransactionCategoryGroupId}' not found.");

        try
        {
            var category = new TransactionCategory(command.Name, group);
            var result = await transactionCategoryRepository.AddTransactionCategoryAsync(category, command.TransactionCategoryGroupId, cancellationToken);

            if (!result.IsSuccess)
                return Result<TransactionCategory>.Failure(result.Error);

            category.Id = result.Value;
            return Result<TransactionCategory>.Success(category);
        }
        catch (ArgumentException ex)
        {
            return Result<TransactionCategory>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateTransactionCategoryAsync(UpdateTransactionCategoryCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Update transaction category command cannot be null.");

        var group = await transactionCategoryGroupRepository.GetTransactionCategoryGroupByIdAsync(command.TransactionCategoryGroupId, cancellationToken);
        if (group is null)
            return Result.Failure($"Transaction category group with ID '{command.TransactionCategoryGroupId}' not found.");

        try
        {
            var category = new TransactionCategory(command.TransactionCategoryId, command.Name, group);
            return await transactionCategoryRepository.UpdateTransactionCategoryAsync(category, command.TransactionCategoryGroupId, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteTransactionCategoryAsync(int transactionCategoryId, CancellationToken cancellationToken = default)
        => await transactionCategoryRepository.DeleteTransactionCategoryAsync(transactionCategoryId, cancellationToken);

    public async Task<Result> ReorderTransactionCategoriesAsync(ReorderTransactionCategoriesCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Reorder transaction categories command cannot be null.");

        return await transactionCategoryRepository.ReorderTransactionCategoriesAsync(command.OrderedTransactionCategoryIds, cancellationToken);
    }
}