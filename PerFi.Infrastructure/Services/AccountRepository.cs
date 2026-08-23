using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class AccountRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : IAccountRepository
{
    public async Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.Institution!.UserId == currentUserService.UserId)
            .Include(a => a.AccountType)
                .ThenInclude(t => t.AccountTypeGroup)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Select(a => new Account(
                a.Id,
                a.Name,
                new AccountType(
                    a.AccountType.Id,
                    a.AccountType.Name,
                    new AccountTypeGroup(a.AccountType.AccountTypeGroup.Id, a.AccountType.AccountTypeGroup.Name)
                    {
                        DisplayOrder = a.AccountType.AccountTypeGroup.DisplayOrder
                    })
                {
                    DisplayOrder = a.AccountType.DisplayOrder
                },
                a.InstitutionId)
            {
                DisplayOrder = a.DisplayOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var accountEntity = await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.AccountType)
                .ThenInclude(t => t.AccountTypeGroup)
            .FirstOrDefaultAsync(a => a.Id == id && a.Institution!.UserId == currentUserService.UserId, cancellationToken);

        if (accountEntity == null)
            return null;

        var group = new AccountTypeGroup(accountEntity.AccountType.AccountTypeGroup.Id, accountEntity.AccountType.AccountTypeGroup.Name)
        {
            DisplayOrder = accountEntity.AccountType.AccountTypeGroup.DisplayOrder
        };

        var type = new AccountType(accountEntity.AccountType.Id, accountEntity.AccountType.Name, group)
        {
            DisplayOrder = accountEntity.AccountType.DisplayOrder
        };

        return new Account(accountEntity.Id, accountEntity.Name, type, accountEntity.InstitutionId)
        {
            DisplayOrder = accountEntity.DisplayOrder
        };
    }

    public async Task<Result<int>> AddAccountAsync(Account account, int institutionId, CancellationToken cancellationToken = default)
    {
        var institution = await dbContext.Institutions
            .FirstOrDefaultAsync(i => i.Id == institutionId && i.UserId == currentUserService.UserId, cancellationToken);

        if (institution == null)
            return Result<int>.Failure($"Institution with ID '{institutionId}' does not exist.");

        var accountType = await dbContext.AccountTypes
            .FirstOrDefaultAsync(at => at.Id == account.Type.Id, cancellationToken);

        if (accountType == null)
            return Result<int>.Failure($"Account type with ID '{account.Type.Id}' does not exist.");

        var nextDisplayOrder = await dbContext.Accounts
            .Where(accountEntity => accountEntity.Institution!.UserId == currentUserService.UserId)
            .Select(accountEntity => (int?)accountEntity.DisplayOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var newAccount = new AccountEntity
        {
            Name = account.Name,
            DisplayOrder = nextDisplayOrder + 1,
            InstitutionId = institution.Id,
            AccountTypeId = accountType.Id,
            AccountType = accountType
        };

        institution.Accounts.Add(newAccount);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(newAccount.Id);
    }

    public async Task<Result> UpdateAccountAsync(Account account, int institutionId, CancellationToken cancellationToken = default)
    {
        var accountEntity = await dbContext.Accounts
            .Include(a => a.AccountType)
            .FirstOrDefaultAsync(a => a.Id == account.Id && a.Institution!.UserId == currentUserService.UserId, cancellationToken);

        if (accountEntity is null)
            return Result.Failure($"Account with ID '{account.Id}' not found.");

        var institution = await dbContext.Institutions
            .FirstOrDefaultAsync(i => i.Id == institutionId && i.UserId == currentUserService.UserId, cancellationToken);

        if (institution is null)
            return Result.Failure($"Institution with ID '{institutionId}' does not exist.");

        var accountType = await dbContext.AccountTypes
            .FirstOrDefaultAsync(at => at.Id == account.Type.Id, cancellationToken);

        if (accountType is null)
            return Result.Failure($"Account type with ID '{account.Type.Id}' does not exist.");

        accountEntity.Name = account.Name;
        accountEntity.AccountType = accountType;
        accountEntity.AccountTypeId = accountType.Id;
        accountEntity.InstitutionId = institution.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var accountEntity = await dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.Institution!.UserId == currentUserService.UserId, cancellationToken);

        if (accountEntity is null)
            return Result.Failure($"Account with ID '{accountId}' not found.");

        var hasBalances = await dbContext.AccountBalances
            .AnyAsync(ab => ab.AccountId == accountId, cancellationToken);

        if (hasBalances)
            return Result.Failure("Cannot delete account because it is referenced by one or more snapshot balances.");

        dbContext.Accounts.Remove(accountEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReorderAccountsAsync(IReadOnlyList<int> orderedAccountIds, CancellationToken cancellationToken = default)
    {
        var normalizedIds = orderedAccountIds.Distinct().ToList();
        if (normalizedIds.Count != orderedAccountIds.Count)
            return Result.Failure("Account reorder list contains duplicate IDs.");

        var accountEntities = await dbContext.Accounts
            .Where(account => normalizedIds.Contains(account.Id) && account.Institution!.UserId == currentUserService.UserId)
            .ToListAsync(cancellationToken);

        if (accountEntities.Count != normalizedIds.Count)
            return Result.Failure("One or more accounts in the reorder list do not exist.");

        var entitiesById = accountEntities.ToDictionary(account => account.Id);

        for (var index = 0; index < normalizedIds.Count; index++)
        {
            entitiesById[normalizedIds[index]].DisplayOrder = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}