using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class TransactionCategoryRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : ITransactionCategoryRepository
{
    public async Task<IReadOnlyList<TransactionCategory>> GetAllTransactionCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TransactionCategories
            .AsNoTracking()
            .Where(category => category.TransactionCategoryGroup.UserId == currentUserService.UserId)
            .Include(category => category.TransactionCategoryGroup)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new TransactionCategory(
                category.Id,
                category.Name,
                new TransactionCategoryGroup(category.TransactionCategoryGroup.Id, category.TransactionCategoryGroup.Name)
                {
                    DisplayOrder = category.TransactionCategoryGroup.DisplayOrder
                })
            {
                DisplayOrder = category.DisplayOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TransactionCategory?> GetTransactionCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var categoryEntity = await dbContext.TransactionCategories
            .AsNoTracking()
            .Include(category => category.TransactionCategoryGroup)
            .FirstOrDefaultAsync(
                category => category.Id == id && category.TransactionCategoryGroup.UserId == currentUserService.UserId,
                cancellationToken);

        return categoryEntity is null
            ? null
            : new TransactionCategory(
                categoryEntity.Id,
                categoryEntity.Name,
                new TransactionCategoryGroup(categoryEntity.TransactionCategoryGroup.Id, categoryEntity.TransactionCategoryGroup.Name)
                {
                    DisplayOrder = categoryEntity.TransactionCategoryGroup.DisplayOrder
                })
            {
                DisplayOrder = categoryEntity.DisplayOrder
            };
    }

    public async Task<Result<int>> AddTransactionCategoryAsync(TransactionCategory transactionCategory, int transactionCategoryGroupId, CancellationToken cancellationToken = default)
    {
        if (await dbContext.TransactionCategories.AnyAsync(
                category => category.Name == transactionCategory.Name && category.TransactionCategoryGroup.UserId == currentUserService.UserId,
                cancellationToken))
        {
            return Result<int>.Failure($"A transaction category with name '{transactionCategory.Name}' already exists.");
        }

        var groupEntity = await dbContext.TransactionCategoryGroups
            .FirstOrDefaultAsync(
                group => group.Id == transactionCategoryGroupId && group.UserId == currentUserService.UserId,
                cancellationToken);

        if (groupEntity is null)
            return Result<int>.Failure($"Transaction category group with ID '{transactionCategoryGroupId}' does not exist.");

        var nextDisplayOrder = await dbContext.TransactionCategories
            .Where(category => category.TransactionCategoryGroup.UserId == currentUserService.UserId)
            .Select(category => (int?)category.DisplayOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var categoryEntity = new TransactionCategoryEntity
        {
            Name = transactionCategory.Name,
            DisplayOrder = nextDisplayOrder + 1,
            UserId = currentUserService.UserId,
            TransactionCategoryGroupId = groupEntity.Id,
            TransactionCategoryGroup = groupEntity,
            Transactions = []
        };

        dbContext.TransactionCategories.Add(categoryEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(categoryEntity.Id);
    }

    public async Task<Result> UpdateTransactionCategoryAsync(TransactionCategory transactionCategory, int transactionCategoryGroupId, CancellationToken cancellationToken = default)
    {
        var categoryEntity = await dbContext.TransactionCategories
            .Include(category => category.TransactionCategoryGroup)
            .FirstOrDefaultAsync(
                category => category.Id == transactionCategory.Id && category.TransactionCategoryGroup.UserId == currentUserService.UserId,
                cancellationToken);

        if (categoryEntity is null)
            return Result.Failure($"Transaction category with ID '{transactionCategory.Id}' not found.");

        var hasDuplicateName = await dbContext.TransactionCategories
            .AnyAsync(
                category => category.Id != transactionCategory.Id
                            && category.Name == transactionCategory.Name
                            && category.TransactionCategoryGroup.UserId == currentUserService.UserId,
                cancellationToken);

        if (hasDuplicateName)
            return Result.Failure($"A transaction category with name '{transactionCategory.Name}' already exists.");

        var groupEntity = await dbContext.TransactionCategoryGroups
            .FirstOrDefaultAsync(
                group => group.Id == transactionCategoryGroupId && group.UserId == currentUserService.UserId,
                cancellationToken);

        if (groupEntity is null)
            return Result.Failure($"Transaction category group with ID '{transactionCategoryGroupId}' does not exist.");

        categoryEntity.Name = transactionCategory.Name;
        categoryEntity.TransactionCategoryGroupId = groupEntity.Id;
        categoryEntity.TransactionCategoryGroup = groupEntity;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteTransactionCategoryAsync(int transactionCategoryId, CancellationToken cancellationToken = default)
    {
        var categoryEntity = await dbContext.TransactionCategories
            .FirstOrDefaultAsync(
                category => category.Id == transactionCategoryId && category.TransactionCategoryGroup.UserId == currentUserService.UserId,
                cancellationToken);

        if (categoryEntity is null)
            return Result.Failure($"Transaction category with ID '{transactionCategoryId}' not found.");

        var isReferenced = await dbContext.Transactions
            .AnyAsync(transaction => transaction.TransactionCategoryId == transactionCategoryId, cancellationToken);

        if (isReferenced)
            return Result.Failure("Cannot delete transaction category because one or more transactions reference it.");

        dbContext.TransactionCategories.Remove(categoryEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReorderTransactionCategoriesAsync(IReadOnlyList<int> orderedTransactionCategoryIds, CancellationToken cancellationToken = default)
    {
        var normalizedIds = orderedTransactionCategoryIds.Distinct().ToList();
        if (normalizedIds.Count != orderedTransactionCategoryIds.Count)
            return Result.Failure("Transaction category reorder list contains duplicate IDs.");

        var categoryEntities = await dbContext.TransactionCategories
            .Where(category => normalizedIds.Contains(category.Id) && category.TransactionCategoryGroup.UserId == currentUserService.UserId)
            .ToListAsync(cancellationToken);

        if (categoryEntities.Count != normalizedIds.Count)
            return Result.Failure("One or more transaction categories in the reorder list do not exist.");

        var entitiesById = categoryEntities.ToDictionary(category => category.Id);

        for (var index = 0; index < normalizedIds.Count; index++)
        {
            entitiesById[normalizedIds[index]].DisplayOrder = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
