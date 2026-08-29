using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class AccountTypeRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : IAccountTypeRepository
{
    public async Task<IReadOnlyList<AccountType>> GetAllAccountTypesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AccountTypes
            .AsNoTracking()
            .Where(at => at.AccountTypeGroup.UserId == currentUserService.UserId)
            .Include(at => at.AccountTypeGroup)
            .OrderBy(at => at.DisplayOrder)
            .ThenBy(at => at.Name)
            .ThenBy(at => at.Id)
            .Select(at => new AccountType(
                at.Id,
                at.Name,
                new AccountTypeGroup(at.AccountTypeGroup.Id, at.AccountTypeGroup.Name)
                {
                    DisplayOrder = at.AccountTypeGroup.DisplayOrder
                })
            {
                DisplayOrder = at.DisplayOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountType?> GetAccountTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var accountTypeEntity = await dbContext.AccountTypes
            .Include(at => at.AccountTypeGroup)
            .FirstOrDefaultAsync(at => at.Id == id && at.AccountTypeGroup.UserId == currentUserService.UserId, cancellationToken);

        if (accountTypeEntity == null)
            return null;

        var group = new AccountTypeGroup(accountTypeEntity.AccountTypeGroup.Id, accountTypeEntity.AccountTypeGroup.Name)
        {
            DisplayOrder = accountTypeEntity.AccountTypeGroup.DisplayOrder
        };

        return new AccountType(accountTypeEntity.Id, accountTypeEntity.Name, group)
        {
            DisplayOrder = accountTypeEntity.DisplayOrder
        };
    }

    public async Task<Result<int>> AddAccountTypeAsync(AccountType accountType, int accountTypeGroupId, CancellationToken cancellationToken = default)
    {
        if (await dbContext.AccountTypes.AnyAsync(at => at.Name == accountType.Name && at.AccountTypeGroup.UserId == currentUserService.UserId, cancellationToken))
            return Result<int>.Failure($"An account type with name '{accountType.Name}' already exists.");

        var accountTypeGroup = await dbContext.AccountTypeGroups
            .FirstOrDefaultAsync(group => group.Id == accountTypeGroupId && group.UserId == currentUserService.UserId, cancellationToken);

        if (accountTypeGroup is null)
            return Result<int>.Failure($"Account type group with ID '{accountTypeGroupId}' does not exist.");

        var nextDisplayOrder = await dbContext.AccountTypes
            .Where(accountTypeEntity => accountTypeEntity.AccountTypeGroup.UserId == currentUserService.UserId)
            .Select(accountTypeEntity => (int?)accountTypeEntity.DisplayOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var newAccountType = new AccountTypeEntity
        {
            Name = accountType.Name,
            DisplayOrder = nextDisplayOrder + 1,
            UserId = currentUserService.UserId,
            AccountTypeGroupId = accountTypeGroup.Id,
            AccountTypeGroup = accountTypeGroup
        };

        dbContext.AccountTypes.Add(newAccountType);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(newAccountType.Id);
    }

    public async Task<Result> UpdateAccountTypeAsync(AccountType accountType, int accountTypeGroupId, CancellationToken cancellationToken = default)
    {
        var accountTypeEntity = await dbContext.AccountTypes
            .Include(at => at.AccountTypeGroup)
            .FirstOrDefaultAsync(at => at.Id == accountType.Id && at.AccountTypeGroup.UserId == currentUserService.UserId, cancellationToken);

        if (accountTypeEntity is null)
            return Result.Failure($"Account type with ID '{accountType.Id}' not found.");

        var hasDuplicateName = await dbContext.AccountTypes
            .AnyAsync(at => at.Id != accountType.Id && at.Name == accountType.Name && at.AccountTypeGroup.UserId == currentUserService.UserId, cancellationToken);

        if (hasDuplicateName)
            return Result.Failure($"An account type with name '{accountType.Name}' already exists.");

        var accountTypeGroup = await dbContext.AccountTypeGroups
            .FirstOrDefaultAsync(group => group.Id == accountTypeGroupId && group.UserId == currentUserService.UserId, cancellationToken);

        if (accountTypeGroup is null)
            return Result.Failure($"Account type group with ID '{accountTypeGroupId}' does not exist.");

        accountTypeEntity.Name = accountType.Name;
        accountTypeEntity.AccountTypeGroup = accountTypeGroup;
        accountTypeEntity.AccountTypeGroupId = accountTypeGroup.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAccountTypeAsync(int accountTypeId, CancellationToken cancellationToken = default)
    {
        var accountTypeEntity = await dbContext.AccountTypes
            .FirstOrDefaultAsync(at => at.Id == accountTypeId && at.AccountTypeGroup.UserId == currentUserService.UserId, cancellationToken);

        if (accountTypeEntity is null)
            return Result.Failure($"Account type with ID '{accountTypeId}' not found.");

        var isReferenced = await dbContext.Accounts
            .AnyAsync(a => a.AccountTypeId == accountTypeId, cancellationToken);

        if (isReferenced)
            return Result.Failure("Cannot delete account type because one or more accounts reference it.");

        dbContext.AccountTypes.Remove(accountTypeEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReorderAccountTypesAsync(IReadOnlyList<int> orderedAccountTypeIds, CancellationToken cancellationToken = default)
    {
        var normalizedIds = orderedAccountTypeIds.Distinct().ToList();
        if (normalizedIds.Count != orderedAccountTypeIds.Count)
            return Result.Failure("Account type reorder list contains duplicate IDs.");

        var accountTypeEntities = await dbContext.AccountTypes
            .Where(accountType => normalizedIds.Contains(accountType.Id) && accountType.AccountTypeGroup.UserId == currentUserService.UserId)
            .ToListAsync(cancellationToken);

        if (accountTypeEntities.Count != normalizedIds.Count)
            return Result.Failure("One or more account types in the reorder list do not exist.");

        var entitiesById = accountTypeEntities.ToDictionary(accountType => accountType.Id);

        for (var index = 0; index < normalizedIds.Count; index++)
        {
            entitiesById[normalizedIds[index]].DisplayOrder = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}