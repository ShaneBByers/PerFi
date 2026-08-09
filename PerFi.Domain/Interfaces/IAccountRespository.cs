using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IAccountRepository
{
    Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddAccountAsync(Account account, int institutionId, CancellationToken cancellationToken = default);
    Task<Result> UpdateAccountAsync(Account account, int institutionId, CancellationToken cancellationToken = default);
    Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default);
    Task<Result> ReorderAccountsAsync(IReadOnlyList<int> orderedAccountIds, CancellationToken cancellationToken = default);
}