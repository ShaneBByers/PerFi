using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class TransactionService(
    ITransactionRepository transactionRepository,
    ITransactionCategoryRepository transactionCategoryRepository,
    IAccountRepository accountRepository)
    : ITransactionService
{
    public async Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default)
        => await transactionRepository.GetAllTransactionsAsync(cancellationToken);

    public async Task<Transaction?> GetTransactionByIdAsync(int id, CancellationToken cancellationToken = default)
        => await transactionRepository.GetTransactionByIdAsync(id, cancellationToken);

    public async Task<Result<Transaction>> CreateTransactionAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<Transaction>.Failure("Create transaction command cannot be null.");

        var category = await transactionCategoryRepository.GetTransactionCategoryByIdAsync(command.TransactionCategoryId, cancellationToken);
        if (category is null)
            return Result<Transaction>.Failure($"Transaction category with ID '{command.TransactionCategoryId}' not found.");

        var account = await accountRepository.GetAccountByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
            return Result<Transaction>.Failure($"Account with ID '{command.AccountId}' not found.");

        try
        {
            var transaction = new Transaction(command.Date, command.CounterpartyName, command.Amount, category, command.AccountId, command.Description);
            var result = await transactionRepository.AddTransactionAsync(transaction, cancellationToken);

            if (!result.IsSuccess)
                return Result<Transaction>.Failure(result.Error);

            transaction.Id = result.Value;
            return Result<Transaction>.Success(transaction);
        }
        catch (ArgumentException ex)
        {
            return Result<Transaction>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateTransactionAsync(UpdateTransactionCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Update transaction command cannot be null.");

        var category = await transactionCategoryRepository.GetTransactionCategoryByIdAsync(command.TransactionCategoryId, cancellationToken);
        if (category is null)
            return Result.Failure($"Transaction category with ID '{command.TransactionCategoryId}' not found.");

        var account = await accountRepository.GetAccountByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
            return Result.Failure($"Account with ID '{command.AccountId}' not found.");

        try
        {
            var transaction = new Transaction(command.TransactionId, command.Date, command.CounterpartyName, command.Amount, category, command.AccountId, command.Description);
            return await transactionRepository.UpdateTransactionAsync(transaction, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteTransactionAsync(int transactionId, CancellationToken cancellationToken = default)
        => await transactionRepository.DeleteTransactionAsync(transactionId, cancellationToken);
}