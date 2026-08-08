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
            .Select(a => new Account(a.Id, a.Name, new AccountType(a.Type.Id, a.Type.Name)))
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

        return new Account(accountEntity.Id, accountEntity.Name, new AccountType(accountEntity.Type.Id, accountEntity.Type.Name));
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
}