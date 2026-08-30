using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class TransactionCategoryGroupRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : ITransactionCategoryGroupRepository
{
    public async Task<IReadOnlyList<TransactionCategoryGroup>> GetAllTransactionCategoryGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TransactionCategoryGroups
            .AsNoTracking()
            .Where(group => group.UserId == currentUserService.UserId)
            .OrderBy(group => group.DisplayOrder)
            .ThenBy(group => group.Name)
            .ThenBy(group => group.Id)
            .Select(group => new TransactionCategoryGroup(group.Id, group.Name)
            {
                DisplayOrder = group.DisplayOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TransactionCategoryGroup?> GetTransactionCategoryGroupByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var groupEntity = await dbContext.TransactionCategoryGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(
                group => group.Id == id && group.UserId == currentUserService.UserId,
                cancellationToken);

        return groupEntity is null
            ? null
            : new TransactionCategoryGroup(groupEntity.Id, groupEntity.Name)
            {
                DisplayOrder = groupEntity.DisplayOrder
            };
    }

    public async Task<Result<int>> AddTransactionCategoryGroupAsync(TransactionCategoryGroup transactionCategoryGroup, CancellationToken cancellationToken = default)
    {
        if (await dbContext.TransactionCategoryGroups.AnyAsync(
                group => group.Name == transactionCategoryGroup.Name && group.UserId == currentUserService.UserId,
                cancellationToken))
        {
            return Result<int>.Failure($"A transaction category group with name '{transactionCategoryGroup.Name}' already exists.");
        }

        var nextDisplayOrder = await dbContext.TransactionCategoryGroups
            .Where(group => group.UserId == currentUserService.UserId)
            .Select(group => (int?)group.DisplayOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var groupEntity = new TransactionCategoryGroupEntity
        {
            Name = transactionCategoryGroup.Name,
            DisplayOrder = nextDisplayOrder + 1,
            UserId = currentUserService.UserId,
            TransactionCategories = []
        };

        dbContext.TransactionCategoryGroups.Add(groupEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(groupEntity.Id);
    }

    public async Task<Result> UpdateTransactionCategoryGroupAsync(TransactionCategoryGroup transactionCategoryGroup, CancellationToken cancellationToken = default)
    {
        var groupEntity = await dbContext.TransactionCategoryGroups
            .FirstOrDefaultAsync(
                group => group.Id == transactionCategoryGroup.Id && group.UserId == currentUserService.UserId,
                cancellationToken);

        if (groupEntity is null)
            return Result.Failure($"Transaction category group with ID '{transactionCategoryGroup.Id}' not found.");

        var hasDuplicateName = await dbContext.TransactionCategoryGroups
            .AnyAsync(
                group => group.Id != transactionCategoryGroup.Id
                         && group.Name == transactionCategoryGroup.Name
                         && group.UserId == currentUserService.UserId,
                cancellationToken);

        if (hasDuplicateName)
            return Result.Failure($"A transaction category group with name '{transactionCategoryGroup.Name}' already exists.");

        groupEntity.Name = transactionCategoryGroup.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteTransactionCategoryGroupAsync(int transactionCategoryGroupId, CancellationToken cancellationToken = default)
    {
        var groupEntity = await dbContext.TransactionCategoryGroups
            .FirstOrDefaultAsync(
                group => group.Id == transactionCategoryGroupId && group.UserId == currentUserService.UserId,
                cancellationToken);

        if (groupEntity is null)
            return Result.Failure($"Transaction category group with ID '{transactionCategoryGroupId}' not found.");

        var isReferenced = await dbContext.TransactionCategories
            .AnyAsync(category => category.TransactionCategoryGroupId == transactionCategoryGroupId, cancellationToken);

        if (isReferenced)
            return Result.Failure("Cannot delete transaction category group because one or more transaction categories reference it.");

        dbContext.TransactionCategoryGroups.Remove(groupEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ReorderTransactionCategoryGroupsAsync(IReadOnlyList<int> orderedTransactionCategoryGroupIds, CancellationToken cancellationToken = default)
    {
        var normalizedIds = orderedTransactionCategoryGroupIds.Distinct().ToList();
        if (normalizedIds.Count != orderedTransactionCategoryGroupIds.Count)
            return Result.Failure("Transaction category group reorder list contains duplicate IDs.");

        var groupEntities = await dbContext.TransactionCategoryGroups
            .Where(group => normalizedIds.Contains(group.Id) && group.UserId == currentUserService.UserId)
            .ToListAsync(cancellationToken);

        if (groupEntities.Count != normalizedIds.Count)
            return Result.Failure("One or more transaction category groups in the reorder list do not exist.");

        var entitiesById = groupEntities.ToDictionary(group => group.Id);

        for (var index = 0; index < normalizedIds.Count; index++)
        {
            entitiesById[normalizedIds[index]].DisplayOrder = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
