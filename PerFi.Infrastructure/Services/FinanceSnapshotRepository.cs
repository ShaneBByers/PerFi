using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Entities.Enums;
using PerFi.Domain.Interfaces;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

public class FinanceSnapshotRepository(PerFiDbContext dbContext)
    : IFinanceSnapshotRepository
{
    public async Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.FinanceSnapshots
            .Include(s => s.AccountBalances)
                .ThenInclude(ab => ab.Account)
                    .ThenInclude(a => a.Institution)
            .Include(s => s.AccountBalances)
                .ThenInclude(ab => ab.Account)
                    .ThenInclude(a => a.AccountType)
            .Select(s => new FinanceSnapshot(
                s.Date,
                s.AccountBalances.Select(ab => new AccountBalance(
                    new Account(ab.Account.AccountName, ab.Account.Institution.Name, Enum.Parse<AccountType>(ab.Account.AccountType.Name)),
                    (double)ab.Balance)).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AddSnapshotAsync(DateOnly date, IReadOnlyCollection<AccountBalance> accountBalances, CancellationToken cancellationToken = default)
    {
        var existingAccounts = await dbContext.Accounts
            .ToListAsync(cancellationToken);

        var snapshotEntity = new FinanceSnapshotEntity
        {
            Date = date,
            AccountBalances = [.. accountBalances.Select(ab => new AccountBalanceEntity
            {
                Account = existingAccounts.First(a => a.AccountName == ab.Account.AccountName),
                Balance = (decimal)ab.Balance
            })]
        };

        dbContext.FinanceSnapshots.Add(snapshotEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}