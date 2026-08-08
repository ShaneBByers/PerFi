using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Entities.Enums;
using PerFi.Domain.Interfaces;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class AccountRepository(PerFiDbContext dbContext) : IAccountRepository
{
    private readonly PerFiDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts
            .Include(a => a.Institution)
            .Include(a => a.AccountType)
            .Select(a => new Account(a.AccountName, a.Institution.Name, Enum.Parse<AccountType>(a.AccountType.Name)))
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetAccountByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var accountEntity = await _dbContext.Accounts
            .Include(a => a.Institution)
            .Include(a => a.AccountType)
            .FirstOrDefaultAsync(a => a.AccountName == name, cancellationToken);

        if (accountEntity == null)
            return null;

        return new Account(accountEntity.AccountName, accountEntity.Institution.Name, Enum.Parse<AccountType>(accountEntity.AccountType.Name));
    }

    public async Task<bool> AddAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        var institution = await _dbContext.Institutions
            .FirstOrDefaultAsync(i => i.Name == account.InstitutionName, cancellationToken);

        if (institution == null)
        {
            institution = new InstitutionEntity { Name = account.InstitutionName };
            _dbContext.Institutions.Add(institution);
        }

        var accountType = await _dbContext.AccountTypes
            .FirstOrDefaultAsync(at => at.Name == account.AccountType.ToString(), cancellationToken);

        if (accountType == null)
        {
            accountType = new AccountTypeEntity { Name = account.AccountType.ToString() };
            _dbContext.AccountTypes.Add(accountType);
        }

        var newAccount = new AccountEntity
        {
            AccountName = account.AccountName,
            Institution = institution,
            AccountType = accountType
        };

        _dbContext.Accounts.Add(newAccount);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}