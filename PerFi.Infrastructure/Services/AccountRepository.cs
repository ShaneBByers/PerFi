using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class AccountRepository(
    PerFiDbContext dbContext)
    : IAccountRepository
{
    public async Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.AccountType)
                .ThenInclude(t => t.AccountTypeGroup)
            .Select(a => new Account(a.Id, a.Name, new AccountType(a.AccountType.Id, a.AccountType.Name, new AccountTypeGroup(a.AccountType.AccountTypeGroup.Id, a.AccountType.AccountTypeGroup.Name)), a.InstitutionId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var accountEntity = await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.AccountType)
                .ThenInclude(t => t.AccountTypeGroup)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (accountEntity == null)
            return null;

        return new Account(accountEntity.Id, accountEntity.Name, new AccountType(accountEntity.AccountType.Id, accountEntity.AccountType.Name, new AccountTypeGroup(accountEntity.AccountType.AccountTypeGroup.Id, accountEntity.AccountType.AccountTypeGroup.Name)), accountEntity.InstitutionId);
    }

    public async Task<Result<int>> AddAccountAsync(Account account, int institutionId, CancellationToken cancellationToken = default)
    {
        var institution = await dbContext.Institutions
            .FirstOrDefaultAsync(i => i.Id == institutionId, cancellationToken);

        if (institution == null)
            return Result<int>.Failure($"Institution with ID '{institutionId}' does not exist.");

        var accountType = await dbContext.AccountTypes
            .FirstOrDefaultAsync(at => at.Id == account.Type.Id, cancellationToken);

        if (accountType == null)
            return Result<int>.Failure($"Account type with ID '{account.Type.Id}' does not exist.");

        var newAccount = new AccountEntity
        {
            Name = account.Name,
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
            .FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken);

        if (accountEntity is null)
            return Result.Failure($"Account with ID '{account.Id}' not found.");

        var institution = await dbContext.Institutions
            .FirstOrDefaultAsync(i => i.Id == institutionId, cancellationToken);

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
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

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
}