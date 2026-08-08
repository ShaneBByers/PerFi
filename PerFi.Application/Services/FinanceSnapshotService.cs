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
        if (command.AccountIdToBalanceMap is null || command.AccountIdToBalanceMap.Count == 0)
            return Result<FinanceSnapshot>.Failure("A snapshot must contain at least one account balance.");

        var accountBalances = new List<AccountBalance>();
        foreach (var (accountId, balance) in command.AccountIdToBalanceMap)
        {
            var account = await accountRepository.GetAccountByIdAsync(accountId, cancellationToken);

            if (account is null)
                return Result<FinanceSnapshot>.Failure($"Account with ID '{accountId}' does not exist.");

            try
            {
                accountBalances.Add(new AccountBalance(account, balance));
            }
            catch (ArgumentException ex) { return Result<FinanceSnapshot>.Failure(ex.Message); }
        }

        FinanceSnapshot snapshot;
        try
        {
            snapshot = new FinanceSnapshot(command.SnapshotDate, accountBalances);
        }
        catch (ArgumentException ex) { return Result<FinanceSnapshot>.Failure(ex.Message); }

        var result = await financeSnapshotRepository.AddSnapshotAsync(snapshot, cancellationToken);
        
        if (!result.IsSuccess)
            return Result<FinanceSnapshot>.Failure(result.Error);

        snapshot.Id = result.Value;

        return Result<FinanceSnapshot>.Success(snapshot);
    }
}