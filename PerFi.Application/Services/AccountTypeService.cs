using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class AccountTypeService(
    IAccountTypeRepository accountTypeRepository,
    IAccountTypeGroupRepository accountTypeGroupRepository)
    : IAccountTypeService
{
    public async Task<IReadOnlyList<AccountType>> GetAllAccountTypesAsync(CancellationToken cancellationToken = default)
        => await accountTypeRepository.GetAllAccountTypesAsync(cancellationToken);

    public async Task<AccountType?> GetAccountTypeByIdAsync(int id, CancellationToken cancellationToken = default)
        => await accountTypeRepository.GetAccountTypeByIdAsync(id, cancellationToken);

    public async Task<Result<AccountType>> CreateAccountTypeAsync(CreateAccountTypeCommand command, CancellationToken cancellationToken = default)
    {
        var accountTypeGroup = await accountTypeGroupRepository.GetAccountTypeGroupByIdAsync(command.AccountTypeGroupId, cancellationToken);
        if (accountTypeGroup is null)
            return Result<AccountType>.Failure($"Account type group with ID '{command.AccountTypeGroupId}' not found.");

        AccountType accountType;

        try
        {
            accountType = new AccountType(command.AccountTypeName, accountTypeGroup);
        }
        catch (ArgumentException ex) { return Result<AccountType>.Failure(ex.Message); }

        Result<int> result = await accountTypeRepository.AddAccountTypeAsync(
            accountType,
            command.AccountTypeGroupId,
            cancellationToken);

        if (!result.IsSuccess)
            return Result<AccountType>.Failure(result.Error);

        accountType.Id = result.Value;

        return Result<AccountType>.Success(accountType);
    }

    public async Task<Result> UpdateAccountTypeAsync(UpdateAccountTypeCommand command, CancellationToken cancellationToken = default)
    {
        var accountTypeGroup = await accountTypeGroupRepository.GetAccountTypeGroupByIdAsync(command.AccountTypeGroupId, cancellationToken);
        if (accountTypeGroup is null)
            return Result.Failure($"Account type group with ID '{command.AccountTypeGroupId}' not found.");

        try
        {
            var accountType = new AccountType(command.AccountTypeId, command.AccountTypeName, accountTypeGroup);
            return await accountTypeRepository.UpdateAccountTypeAsync(accountType, command.AccountTypeGroupId, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAccountTypeAsync(int accountTypeId, CancellationToken cancellationToken = default)
        => await accountTypeRepository.DeleteAccountTypeAsync(accountTypeId, cancellationToken);

    public async Task<Result> ReorderAccountTypesAsync(ReorderAccountTypeCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Reorder account types command cannot be null.");

        return await accountTypeRepository.ReorderAccountTypesAsync(command.OrderedAccountTypeIds, cancellationToken);
    }
}