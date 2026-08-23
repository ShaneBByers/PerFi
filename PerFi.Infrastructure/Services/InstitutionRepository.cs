using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class InstitutionRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : IInstitutionRepository
{
    public async Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default)
    {
        var institutionEntities = await dbContext.Institutions
            .AsNoTracking()
            .Where(i => i.UserId == currentUserService.UserId)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.Name)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        var accountTypeGroupRows = await dbContext.AccountTypeGroups
            .AsNoTracking()
            .Select(group => new AccountTypeGroupRow(group.Id, group.Name))
            .ToListAsync(cancellationToken);

        var accountTypeRows = await dbContext.AccountTypes
            .AsNoTracking()
            .Select(accountType => new AccountTypeRow(accountType.Id, accountType.Name, accountType.DisplayOrder, accountType.AccountTypeGroupId))
            .ToListAsync(cancellationToken);

        var accountRows = await dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.Institution!.UserId == currentUserService.UserId)
            .OrderBy(account => account.DisplayOrder)
            .ThenBy(account => account.Name)
            .ThenBy(account => account.Id)
            .Select(account => new AccountRow(account.Id, account.Name, account.DisplayOrder, account.InstitutionId, account.AccountTypeId))
            .ToListAsync(cancellationToken);

        var accountTypesById = accountTypeRows.ToDictionary(row => row.Id);
        var accountTypeGroupsById = accountTypeGroupRows.ToDictionary(row => row.Id);

        var accountsByInstitutionId = accountRows
            .GroupBy(account => account.InstitutionId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return institutionEntities
            .Select(i => new Institution(i.Id, i.Name, accountsByInstitutionId.TryGetValue(i.Id, out var accounts)
                ? [.. accounts.Select(a =>
                {
                    var accountType = accountTypesById[a.AccountTypeId];
                    var accountTypeGroup = accountTypeGroupsById[accountType.AccountTypeGroupId];

                    return new Account(a.Id, a.Name, new AccountType(accountType.Id, accountType.Name, new AccountTypeGroup(accountTypeGroup.Id, accountTypeGroup.Name)), i.Id)
                {
                    DisplayOrder = a.DisplayOrder
                };
                })]
                : [])
            {
                DisplayOrder = i.DisplayOrder
            })
            .ToList();
    }

    public async Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var institutionEntity = await dbContext.Institutions
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == currentUserService.UserId, cancellationToken);

        if (institutionEntity == null)
            return null;

        var accountRows = await dbContext.Accounts
            .AsNoTracking()
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Where(account => account.InstitutionId == id)
            .Select(account => new AccountRow(account.Id, account.Name, account.DisplayOrder, account.InstitutionId, account.AccountTypeId))
            .ToListAsync(cancellationToken);

        var accountTypeRows = await dbContext.AccountTypes
            .AsNoTracking()
            .Select(accountType => new AccountTypeRow(accountType.Id, accountType.Name, accountType.DisplayOrder, accountType.AccountTypeGroupId))
            .ToListAsync(cancellationToken);

        var accountTypeGroupRows = await dbContext.AccountTypeGroups
            .AsNoTracking()
            .Select(group => new AccountTypeGroupRow(group.Id, group.Name))
            .ToListAsync(cancellationToken);

        var accountTypesById = accountTypeRows.ToDictionary(row => row.Id);
        var accountTypeGroupsById = accountTypeGroupRows.ToDictionary(row => row.Id);

        return new Institution(institutionEntity.Id, institutionEntity.Name, [.. accountRows
            .Select(a =>
            {
                var accountType = accountTypesById[a.AccountTypeId];
                var accountTypeGroup = accountTypeGroupsById[accountType.AccountTypeGroupId];

                return new Account(a.Id, a.Name, new AccountType(accountType.Id, accountType.Name, new AccountTypeGroup(accountTypeGroup.Id, accountTypeGroup.Name)), institutionEntity.Id)
            {
                DisplayOrder = a.DisplayOrder
            };
            })])
        {
            DisplayOrder = institutionEntity.DisplayOrder
        };
    }

    public async Task<Result<int>> AddInstitutionAsync(Institution institution, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Institutions.AnyAsync(i => i.Name == institution.Name && i.UserId == currentUserService.UserId, cancellationToken))
            return Result<int>.Failure($"An institution with name '{institution.Name}' already exists.");

        var nextDisplayOrder = await dbContext.Institutions
            .Where(i => i.UserId == currentUserService.UserId)
            .Select(institutionEntity => (int?)institutionEntity.DisplayOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var newInstitution = new InstitutionEntity
        {
            Name = institution.Name,
            DisplayOrder = nextDisplayOrder + 1,
            UserId = currentUserService.UserId,
            Accounts = [.. institution.Accounts.Select(a => new AccountEntity
            {
                Name = a.Name,
                DisplayOrder = a.DisplayOrder,
                AccountType = new AccountTypeEntity
                {
                    Name = a.Type.Name,
                    DisplayOrder = a.Type.DisplayOrder,
                    AccountTypeGroup = new AccountTypeGroupEntity
                    {
                        Name = a.Type.Group.Name,
                        DisplayOrder = a.Type.Group.DisplayOrder,
                        UserId = currentUserService.UserId,
                        AccountTypes = []
                    }
                }
            })]
        };

        dbContext.Institutions.Add(newInstitution);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(newInstitution.Id);
    }

    public async Task<Result> UpdateInstitutionAsync(Institution institution, CancellationToken cancellationToken = default)
    {
        var institutionEntity = await dbContext.Institutions
            .FirstOrDefaultAsync(i => i.Id == institution.Id && i.UserId == currentUserService.UserId, cancellationToken);

        if (institutionEntity is null)
            return Result.Failure($"Institution with ID '{institution.Id}' not found.");

        var hasDuplicateName = await dbContext.Institutions
            .AnyAsync(i => i.Id != institution.Id && i.Name == institution.Name && i.UserId == currentUserService.UserId, cancellationToken);

        if (hasDuplicateName)
            return Result.Failure($"An institution with name '{institution.Name}' already exists.");

        institutionEntity.Name = institution.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteInstitutionAsync(int institutionId, CancellationToken cancellationToken = default)
    {
        var institutionEntity = await dbContext.Institutions
            .FirstOrDefaultAsync(i => i.Id == institutionId && i.UserId == currentUserService.UserId, cancellationToken);

        if (institutionEntity is null)
            return Result.Failure($"Institution with ID '{institutionId}' not found.");

        var hasAccounts = await dbContext.Accounts
            .AnyAsync(a => a.InstitutionId == institutionId, cancellationToken);

        if (hasAccounts)
            return Result.Failure("Cannot delete institution because one or more accounts reference it.");

        dbContext.Institutions.Remove(institutionEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReorderInstitutionsAsync(IReadOnlyList<int> orderedInstitutionIds, CancellationToken cancellationToken = default)
    {
        var normalizedIds = orderedInstitutionIds.Distinct().ToList();
        if (normalizedIds.Count != orderedInstitutionIds.Count)
            return Result.Failure("Institution reorder list contains duplicate IDs.");

        var institutionEntities = await dbContext.Institutions
            .Where(institution => normalizedIds.Contains(institution.Id) && institution.UserId == currentUserService.UserId)
            .ToListAsync(cancellationToken);

        if (institutionEntities.Count != normalizedIds.Count)
            return Result.Failure("One or more institutions in the reorder list do not exist.");

        var entitiesById = institutionEntities.ToDictionary(institution => institution.Id);

        for (var index = 0; index < normalizedIds.Count; index++)
        {
            entitiesById[normalizedIds[index]].DisplayOrder = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private sealed record AccountRow(
        int Id,
        string Name,
        int DisplayOrder,
        int InstitutionId,
        int AccountTypeId);

    private sealed record AccountTypeRow(
        int Id,
        string Name,
        int DisplayOrder,
        int AccountTypeGroupId);

    private sealed record AccountTypeGroupRow(
        int Id,
        string Name);
}