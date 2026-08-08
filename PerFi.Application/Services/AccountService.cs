using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class AccountService(
    IAccountRepository accountRepository,
    IAccountTypeRepository accountTypeRepository,
    IInstitutionRepository institutionRepository)
    : IAccountService
{
    public async Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
        => await accountRepository.GetAllAccountsAsync(cancellationToken);

    public async Task<Account?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default)
        => await accountRepository.GetAccountByIdAsync(id, cancellationToken);

    public async Task<Result<Account>> CreateAccountAsync(CreateAccountCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<Account>.Failure("Create account command cannot be null.");

        var accountType = await accountTypeRepository.GetAccountTypeByIdAsync(command.AccountTypeId, cancellationToken);
        if (accountType is null)
            return Result<Account>.Failure($"Account type with ID '{command.AccountTypeId}' not found.");

        var institution = await institutionRepository.GetInstitutionByIdAsync(command.InstitutionId, cancellationToken);
        if (institution is null)
            return Result<Account>.Failure($"Institution with ID '{command.InstitutionId}' not found.");

        try
        {
            var account = new Account(command.AccountName, accountType);
            var result = await accountRepository.AddAccountAsync(account, command.InstitutionId, cancellationToken);

            if (!result.IsSuccess)
                return Result<Account>.Failure(result.Error);

            account.Id = result.Value;
            return Result<Account>.Success(account);
        }
        catch (ArgumentException ex)
        {
            return Result<Account>.Failure(ex.Message);
        }
    }
}