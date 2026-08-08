using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;

namespace PerFi.Application.Services;

internal class AccountService(
    IAccountRepository accountRepository) : IAccountService
{
    public async Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await accountRepository.GetAllAccountsAsync(cancellationToken);
    }

    public async Task<Account?> GetAccountByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await accountRepository.GetAccountByNameAsync(name, cancellationToken);
    }

    public async Task<bool> CreateAccountAsync(CreateAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = new Account(command.AccountName, command.InstitutionName, command.AccountType);
        return await accountRepository.AddAccountAsync(account, cancellationToken);
    }
}