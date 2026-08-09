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

    public async Task<Result> UpdateAccountAsync(UpdateAccountCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await accountRepository.GetAccountByIdAsync(command.AccountId, cancellationToken);
        if (existing is null)
            return Result.Failure($"Account with ID '{command.AccountId}' not found.");

        var accountType = await accountTypeRepository.GetAccountTypeByIdAsync(command.AccountTypeId, cancellationToken);
        if (accountType is null)
            return Result.Failure($"Account type with ID '{command.AccountTypeId}' not found.");

        var institution = await institutionRepository.GetInstitutionByIdAsync(command.InstitutionId, cancellationToken);
        if (institution is null)
            return Result.Failure($"Institution with ID '{command.InstitutionId}' not found.");

        try
        {
            var account = new Account(command.AccountId, command.AccountName, accountType);
            return await accountRepository.UpdateAccountAsync(account, command.InstitutionId, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default)
        => await accountRepository.DeleteAccountAsync(accountId, cancellationToken);

    public async Task<Result> ReorderAccountsAsync(ReorderAccountCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Reorder accounts command cannot be null.");

        return await accountRepository.ReorderAccountsAsync(command.OrderedAccountIds, cancellationToken);
    }
}