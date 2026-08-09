using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class AccountTypeGroupRepository(
    PerFiDbContext dbContext)
    : IAccountTypeGroupRepository
{
    public async Task<IReadOnlyList<AccountTypeGroup>> GetAllAccountTypeGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AccountTypeGroups
            .AsNoTracking()
            .Select(group => new AccountTypeGroup(group.Id, group.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountTypeGroup?> GetAccountTypeGroupByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var groupEntity = await dbContext.AccountTypeGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

        return groupEntity is null ? null : new AccountTypeGroup(groupEntity.Id, groupEntity.Name);
    }

    public async Task<Result<int>> AddAccountTypeGroupAsync(AccountTypeGroup accountTypeGroup, CancellationToken cancellationToken = default)
    {
        if (await dbContext.AccountTypeGroups.AnyAsync(group => group.Name == accountTypeGroup.Name, cancellationToken))
            return Result<int>.Failure($"An account type group with name '{accountTypeGroup.Name}' already exists.");

        var entity = new AccountTypeGroupEntity
        {
            Name = accountTypeGroup.Name,
            AccountTypes = []
        };

        dbContext.AccountTypeGroups.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateAccountTypeGroupAsync(AccountTypeGroup accountTypeGroup, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AccountTypeGroups
            .FirstOrDefaultAsync(group => group.Id == accountTypeGroup.Id, cancellationToken);

        if (entity is null)
            return Result.Failure($"Account type group with ID '{accountTypeGroup.Id}' not found.");

        var hasDuplicateName = await dbContext.AccountTypeGroups
            .AnyAsync(group => group.Id != accountTypeGroup.Id && group.Name == accountTypeGroup.Name, cancellationToken);

        if (hasDuplicateName)
            return Result.Failure($"An account type group with name '{accountTypeGroup.Name}' already exists.");

        entity.Name = accountTypeGroup.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAccountTypeGroupAsync(int accountTypeGroupId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AccountTypeGroups
            .FirstOrDefaultAsync(group => group.Id == accountTypeGroupId, cancellationToken);

        if (entity is null)
            return Result.Failure($"Account type group with ID '{accountTypeGroupId}' not found.");

        var isReferenced = await dbContext.AccountTypes
            .AnyAsync(type => type.AccountTypeGroupId == accountTypeGroupId, cancellationToken);

        if (isReferenced)
            return Result.Failure("Cannot delete account type group because one or more account types reference it.");

        dbContext.AccountTypeGroups.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}