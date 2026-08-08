using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;

namespace PerFi.Application.Services;

internal class FinanceSnapshotService(
    IFinanceSnapshotRepository financeSnapshotRepository,
    IAccountRepository accountRepository)
    : IFinanceSnapshotService
{
    public async Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        return await financeSnapshotRepository.GetAllSnapshotsAsync(cancellationToken);
    }

    public async Task<bool> CreateSnapshotAsync(CreateFinanceSnapshotCommand command, CancellationToken cancellationToken = default)
    {
        var accountBalances = command.AccountNameToBalanceMap.Select(async kvp =>
        {
            var account = await accountRepository.GetAccountByNameAsync(kvp.Key, cancellationToken)
                ?? throw new ArgumentException($"Account with name '{kvp.Key}' does not exist.");

            return new AccountBalance(account, kvp.Value);
        });

        var snapshot = new FinanceSnapshot(command.SnapshotDate, await Task.WhenAll(accountBalances));

        return await financeSnapshotRepository.AddSnapshotAsync(snapshot, cancellationToken);
    }
}