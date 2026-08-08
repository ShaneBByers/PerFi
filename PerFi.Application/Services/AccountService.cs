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
        var accountType = await accountTypeRepository.GetAccountTypeByIdAsync(command.AccountTypeId, cancellationToken);
        
        if (accountType is null)
            return Result<Account>.Failure($"Account type with ID '{command.AccountTypeId}' not found.");

        var institution = await institutionRepository.GetInstitutionByIdAsync(command.InstitutionId, cancellationToken);

        if (institution is null)
            return Result<Account>.Failure($"Institution with ID '{command.InstitutionId}' not found.");

        Account account;

        try
        {
            account = new Account(command.AccountName, accountType);
        }
        catch (ArgumentException ex) { return Result<Account>.Failure(ex.Message); }

        Result<int> result = await accountRepository.AddAccountAsync(
            account,
            command.InstitutionId,
            cancellationToken);

        if (!result.IsSuccess)
            return Result<Account>.Failure(result.Error);

        account.Id = result.Value;

        return Result<Account>.Success(account);
    }
}