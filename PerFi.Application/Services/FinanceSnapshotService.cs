using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class FinanceSnapshotService(
    IFinanceSnapshotRepository financeSnapshotRepository,
    IAccountRepository accountRepository)
    : IFinanceSnapshotService
{
    public async Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default)
        => await financeSnapshotRepository.GetAllSnapshotsAsync(cancellationToken);

    public async Task<FinanceSnapshot?> GetSnapshotByIdAsync(int id, CancellationToken cancellationToken = default)
        => await financeSnapshotRepository.GetSnapshotByIdAsync(id, cancellationToken);

    public async Task<Result<FinanceSnapshot>> CreateSnapshotAsync(CreateFinanceSnapshotCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<FinanceSnapshot>.Failure("Create snapshot command cannot be null.");

        var accountBalancesResult = await BuildAccountBalancesAsync(command.AccountIdToBalanceMap, cancellationToken);
        if (!accountBalancesResult.IsSuccess)
            return Result<FinanceSnapshot>.Failure(accountBalancesResult.Error ?? "Unable to build account balances.");

        try
        {
            var snapshot = new FinanceSnapshot(command.SnapshotDate, accountBalancesResult.Value!);
            var result = await financeSnapshotRepository.AddSnapshotAsync(snapshot, cancellationToken);

            if (!result.IsSuccess)
                return Result<FinanceSnapshot>.Failure(result.Error);

            snapshot.Id = result.Value;
            return Result<FinanceSnapshot>.Success(snapshot);
        }
        catch (ArgumentException ex)
        {
            return Result<FinanceSnapshot>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateSnapshotAsync(UpdateFinanceSnapshotCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await financeSnapshotRepository.GetSnapshotByIdAsync(command.SnapshotId, cancellationToken);
        if (existing is null)
            return Result.Failure($"Snapshot with ID '{command.SnapshotId}' not found.");

        var accountBalancesResult = await BuildAccountBalancesAsync(command.AccountIdToBalanceMap, cancellationToken);
        if (!accountBalancesResult.IsSuccess)
            return Result.Failure(accountBalancesResult.Error ?? "Unable to build account balances.");

        try
        {
            var snapshot = new FinanceSnapshot(command.SnapshotId, command.SnapshotDate, accountBalancesResult.Value!);
            return await financeSnapshotRepository.UpdateSnapshotAsync(snapshot, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteSnapshotAsync(int snapshotId, CancellationToken cancellationToken = default)
        => await financeSnapshotRepository.DeleteSnapshotAsync(snapshotId, cancellationToken);

    private async Task<Result<IReadOnlyList<AccountBalance>>> BuildAccountBalancesAsync(
        IReadOnlyDictionary<int, decimal>? accountIdToBalanceMap,
        CancellationToken cancellationToken)
    {
        if (accountIdToBalanceMap is null || accountIdToBalanceMap.Count == 0)
            return Result<IReadOnlyList<AccountBalance>>.Failure("A snapshot must contain at least one account balance.");

        var accountBalances = new List<AccountBalance>();

        foreach (var (accountId, balance) in accountIdToBalanceMap)
        {
            var account = await accountRepository.GetAccountByIdAsync(accountId, cancellationToken);
            if (account is null)
                return Result<IReadOnlyList<AccountBalance>>.Failure($"Account with ID '{accountId}' does not exist.");

            try
            {
                accountBalances.Add(new AccountBalance(account, balance));
            }
            catch (ArgumentException ex)
            {
                return Result<IReadOnlyList<AccountBalance>>.Failure(ex.Message);
            }
        }

        return Result<IReadOnlyList<AccountBalance>>.Success(accountBalances);
    }
}