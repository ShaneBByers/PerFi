using PerFi.Application.Commands;
using PerFi.Domain.Entities;

namespace PerFi.Application.Interfaces;

public interface IAccountService
{
    Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> CreateAccountAsync(CreateAccountCommand command, CancellationToken cancellationToken = default);
}