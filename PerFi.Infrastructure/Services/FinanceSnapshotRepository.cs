using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class FinanceSnapshotRepository(PerFiDbContext dbContext)
    : IFinanceSnapshotRepository
{
    public async Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.FinanceSnapshots
            .AsNoTracking()
            .Include(s => s.AccountBalances)
                .ThenInclude(ab => ab.Account)
                    .ThenInclude(a => a.Type)
            .Select(s => new FinanceSnapshot(
                s.Id,
                s.Date,
                s.AccountBalances.Select(ab => new AccountBalance(
                    new Account(ab.Account.Id, ab.Account.Name, new AccountType(ab.Account.Type.Id, ab.Account.Type.Name)),
                    ab.Balance)).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<FinanceSnapshot?> GetSnapshotByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var snapshotEntity = await dbContext.FinanceSnapshots
            .AsNoTracking()
            .Include(s => s.AccountBalances)
                .ThenInclude(ab => ab.Account)
                    .ThenInclude(a => a.Type)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (snapshotEntity == null)
            return null;

        return new FinanceSnapshot(
            snapshotEntity.Id,
            snapshotEntity.Date,
            [.. snapshotEntity.AccountBalances.Select(ab => new AccountBalance(
                new Account(ab.Account.Id, ab.Account.Name, new AccountType(ab.Account.Type.Id, ab.Account.Type.Name)),
                ab.Balance))]);
    }

    public async Task<Result<int>> AddSnapshotAsync(FinanceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existingAccounts = await dbContext.Accounts
            .Where(a => snapshot.AccountBalances.Select(ab => ab.Account.Id).Contains(a.Id))
            .ToListAsync(cancellationToken);

        var snapshotEntity = new FinanceSnapshotEntity
        {
            Date = snapshot.Date,
            AccountBalances = [.. snapshot.AccountBalances.Select(ab => new AccountBalanceEntity
            {
                Account = existingAccounts.First(a => a.Id == ab.Account.Id),
                Balance = ab.Balance
            })]
        };

        dbContext.FinanceSnapshots.Add(snapshotEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(snapshotEntity.Id);
    }
}