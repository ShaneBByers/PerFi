using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class AccountTypeService(
    IAccountTypeRepository accountTypeRepository)
    : IAccountTypeService
{
    public async Task<IReadOnlyList<AccountType>> GetAllAccountTypesAsync(CancellationToken cancellationToken = default)
        => await accountTypeRepository.GetAllAccountTypesAsync(cancellationToken);

    public async Task<AccountType?> GetAccountTypeByIdAsync(int id, CancellationToken cancellationToken = default)
        => await accountTypeRepository.GetAccountTypeByIdAsync(id, cancellationToken);

    public async Task<Result<AccountType>> CreateAccountTypeAsync(CreateAccountTypeCommand command, CancellationToken cancellationToken = default)
    {
        AccountType accountType;

        try
        {
            accountType = new AccountType(command.AccountTypeName);
        }
        catch (ArgumentException ex) { return Result<AccountType>.Failure(ex.Message); }

        Result<int> result = await accountTypeRepository.AddAccountTypeAsync(
            accountType,
            cancellationToken);

        if (!result.IsSuccess)
            return Result<AccountType>.Failure(result.Error);

        accountType.Id = result.Value;

        return Result<AccountType>.Success(accountType);
    }

    public async Task<Result> UpdateAccountTypeAsync(UpdateAccountTypeCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var accountType = new AccountType(command.AccountTypeId, command.AccountTypeName);
            return await accountTypeRepository.UpdateAccountTypeAsync(accountType, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAccountTypeAsync(int accountTypeId, CancellationToken cancellationToken = default)
        => await accountTypeRepository.DeleteAccountTypeAsync(accountTypeId, cancellationToken);
}