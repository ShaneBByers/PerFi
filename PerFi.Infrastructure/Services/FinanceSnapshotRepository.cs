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
                    .ThenInclude(a => a.AccountType)
                        .ThenInclude(t => t.AccountTypeGroup)
            .Select(s => new FinanceSnapshot(
                s.Id,
                s.Date,
                s.AccountBalances.Select(ab => new AccountBalance(
                    new Account(ab.Account.Id, ab.Account.Name, new AccountType(ab.Account.AccountType.Id, ab.Account.AccountType.Name, new AccountTypeGroup(ab.Account.AccountType.AccountTypeGroup.Id, ab.Account.AccountType.AccountTypeGroup.Name)), ab.Account.InstitutionId),
                    ab.Balance)).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<FinanceSnapshot?> GetSnapshotByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var snapshotEntity = await dbContext.FinanceSnapshots
            .AsNoTracking()
            .Include(s => s.AccountBalances)
                .ThenInclude(ab => ab.Account)
                    .ThenInclude(a => a.AccountType)
                        .ThenInclude(t => t.AccountTypeGroup)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (snapshotEntity == null)
            return null;

        return new FinanceSnapshot(
            snapshotEntity.Id,
            snapshotEntity.Date,
            [.. snapshotEntity.AccountBalances.Select(ab => new AccountBalance(
                new Account(ab.Account.Id, ab.Account.Name, new AccountType(ab.Account.AccountType.Id, ab.Account.AccountType.Name, new AccountTypeGroup(ab.Account.AccountType.AccountTypeGroup.Id, ab.Account.AccountType.AccountTypeGroup.Name)), ab.Account.InstitutionId),
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
                AccountId = ab.Account.Id,
                Account = existingAccounts.First(a => a.Id == ab.Account.Id),
                Balance = ab.Balance
            })]
        };

        dbContext.FinanceSnapshots.Add(snapshotEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(snapshotEntity.Id);
    }

    public async Task<Result> UpdateSnapshotAsync(FinanceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var snapshotEntity = await dbContext.FinanceSnapshots
            .Include(s => s.AccountBalances)
            .FirstOrDefaultAsync(s => s.Id == snapshot.Id, cancellationToken);

        if (snapshotEntity is null)
            return Result.Failure($"Snapshot with ID '{snapshot.Id}' not found.");

        var accountIds = snapshot.AccountBalances.Select(ab => ab.Account.Id).Distinct().ToList();
        var existingAccounts = await dbContext.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        if (existingAccounts.Count != accountIds.Count)
            return Result.Failure("One or more accounts in the snapshot do not exist.");

        dbContext.AccountBalances.RemoveRange(snapshotEntity.AccountBalances);

        snapshotEntity.Date = snapshot.Date;
        snapshotEntity.AccountBalances = [.. snapshot.AccountBalances.Select(ab => new AccountBalanceEntity
        {
            AccountId = ab.Account.Id,
            Account = existingAccounts.First(a => a.Id == ab.Account.Id),
            FinanceSnapshotId = snapshotEntity.Id,
            Balance = ab.Balance
        })];

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteSnapshotAsync(int snapshotId, CancellationToken cancellationToken = default)
    {
        var snapshotEntity = await dbContext.FinanceSnapshots
            .Include(s => s.AccountBalances)
            .FirstOrDefaultAsync(s => s.Id == snapshotId, cancellationToken);

        if (snapshotEntity is null)
            return Result.Failure($"Snapshot with ID '{snapshotId}' not found.");

        dbContext.AccountBalances.RemoveRange(snapshotEntity.AccountBalances);
        dbContext.FinanceSnapshots.Remove(snapshotEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}