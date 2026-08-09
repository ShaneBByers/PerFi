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
            .Include(a => a.Type)
            .Select(a => new Account(a.Id, a.Name, new AccountType(a.Type.Id, a.Type.Name), a.InstitutionId ?? 0))
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var accountEntity = await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.Type)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (accountEntity == null)
            return null;

        return new Account(accountEntity.Id, accountEntity.Name, new AccountType(accountEntity.Type.Id, accountEntity.Type.Name), accountEntity.InstitutionId ?? 0);
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
            Type = accountType
        };

        institution.Accounts.Add(newAccount);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(newAccount.Id);
    }

    public async Task<Result> UpdateAccountAsync(Account account, int institutionId, CancellationToken cancellationToken = default)
    {
        var accountEntity = await dbContext.Accounts
            .Include(a => a.Type)
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
        accountEntity.Type = accountType;
        accountEntity.AccountTypeId = accountType.Id;
        accountEntity.InstitutionId = institution.Id;
        dbContext.Entry(accountEntity).Property("TypeId").CurrentValue = accountType.Id;
        dbContext.Entry(accountEntity).Property("InstitutionEntityId").CurrentValue = institution.Id;

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
            .AnyAsync(ab => EF.Property<int>(ab, "AccountId") == accountId, cancellationToken);

        if (hasBalances)
            return Result.Failure("Cannot delete account because it is referenced by one or more snapshot balances.");

        dbContext.Accounts.Remove(accountEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}