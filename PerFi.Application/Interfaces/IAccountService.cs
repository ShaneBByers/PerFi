using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface IAccountService
{
    Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Account>> CreateAccountAsync(CreateAccountCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateAccountAsync(UpdateAccountCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default);
    Task<Result> ReorderAccountsAsync(ReorderAccountCommand command, CancellationToken cancellationToken = default);
}