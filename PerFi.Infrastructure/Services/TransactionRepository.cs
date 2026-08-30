using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class TransactionRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == currentUserService.UserId)
            .OrderBy(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Id)
            .Include(transaction => transaction.TransactionCategory)
                .ThenInclude(category => category.TransactionCategoryGroup)
            .Select(transaction => new Transaction(
                transaction.Id,
                transaction.Date,
                transaction.CounterpartyName,
                transaction.Amount,
                new TransactionCategory(
                    transaction.TransactionCategory.Id,
                    transaction.TransactionCategory.Name,
                    new TransactionCategoryGroup(
                        transaction.TransactionCategory.TransactionCategoryGroup.Id,
                        transaction.TransactionCategory.TransactionCategoryGroup.Name)
                    {
                        DisplayOrder = transaction.TransactionCategory.TransactionCategoryGroup.DisplayOrder
                    })
                {
                    DisplayOrder = transaction.TransactionCategory.DisplayOrder
                },
                transaction.AccountId,
                transaction.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var transactionEntity = await dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.TransactionCategory)
                .ThenInclude(category => category.TransactionCategoryGroup)
            .FirstOrDefaultAsync(
                transaction => transaction.Id == id && transaction.UserId == currentUserService.UserId,
                cancellationToken);

        return transactionEntity is null
            ? null
            : new Transaction(
                transactionEntity.Id,
                transactionEntity.Date,
                transactionEntity.CounterpartyName,
                transactionEntity.Amount,
                new TransactionCategory(
                    transactionEntity.TransactionCategory.Id,
                    transactionEntity.TransactionCategory.Name,
                    new TransactionCategoryGroup(
                        transactionEntity.TransactionCategory.TransactionCategoryGroup.Id,
                        transactionEntity.TransactionCategory.TransactionCategoryGroup.Name)
                    {
                        DisplayOrder = transactionEntity.TransactionCategory.TransactionCategoryGroup.DisplayOrder
                    })
                {
                    DisplayOrder = transactionEntity.TransactionCategory.DisplayOrder
                },
                transactionEntity.AccountId,
                transactionEntity.Description);
    }

    public async Task<Result<int>> AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var transactionCategory = await dbContext.TransactionCategories
            .FirstOrDefaultAsync(
                category => category.Id == transaction.Category.Id && category.UserId == currentUserService.UserId,
                cancellationToken);

        if (transactionCategory is null)
            return Result<int>.Failure($"Transaction category with ID '{transaction.Category.Id}' does not exist.");

        var account = await dbContext.Accounts
            .FirstOrDefaultAsync(
                accountEntity => accountEntity.Id == transaction.AccountId && accountEntity.Institution.UserId == currentUserService.UserId,
                cancellationToken);

        if (account is null)
            return Result<int>.Failure($"Account with ID '{transaction.AccountId}' does not exist.");

        var entity = new TransactionEntity
        {
            Date = transaction.Date,
            CounterpartyName = transaction.CounterpartyName,
            Amount = transaction.Amount,
            Description = transaction.Description,
            UserId = currentUserService.UserId,
            TransactionCategoryId = transactionCategory.Id,
            TransactionCategory = transactionCategory,
            AccountId = account.Id,
            Account = account
        };

        dbContext.Transactions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Transactions
            .FirstOrDefaultAsync(
                transactionEntity => transactionEntity.Id == transaction.Id && transactionEntity.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"Transaction with ID '{transaction.Id}' not found.");

        var transactionCategory = await dbContext.TransactionCategories
            .FirstOrDefaultAsync(
                category => category.Id == transaction.Category.Id && category.UserId == currentUserService.UserId,
                cancellationToken);

        if (transactionCategory is null)
            return Result.Failure($"Transaction category with ID '{transaction.Category.Id}' does not exist.");

        var account = await dbContext.Accounts
            .FirstOrDefaultAsync(
                accountEntity => accountEntity.Id == transaction.AccountId && accountEntity.Institution.UserId == currentUserService.UserId,
                cancellationToken);

        if (account is null)
            return Result.Failure($"Account with ID '{transaction.AccountId}' does not exist.");

        entity.Date = transaction.Date;
        entity.CounterpartyName = transaction.CounterpartyName;
        entity.Amount = transaction.Amount;
        entity.Description = transaction.Description;
        entity.TransactionCategoryId = transactionCategory.Id;
        entity.AccountId = account.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteTransactionAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Transactions
            .FirstOrDefaultAsync(
                transaction => transaction.Id == transactionId && transaction.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"Transaction with ID '{transactionId}' not found.");

        dbContext.Transactions.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
