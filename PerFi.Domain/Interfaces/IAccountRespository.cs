using PerFi.Domain.Entities;

namespace PerFi.Domain.Interfaces;

public interface IAccountRepository
{
    Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<bool> AddAccountAsync(Account account, CancellationToken cancellationToken = default);
}