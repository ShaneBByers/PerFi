using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class AccountTypeGroupService(
    IAccountTypeGroupRepository accountTypeGroupRepository)
    : IAccountTypeGroupService
{
    public async Task<IReadOnlyList<AccountTypeGroup>> GetAllAccountTypeGroupsAsync(CancellationToken cancellationToken = default)
        => await accountTypeGroupRepository.GetAllAccountTypeGroupsAsync(cancellationToken);

    public async Task<AccountTypeGroup?> GetAccountTypeGroupByIdAsync(int id, CancellationToken cancellationToken = default)
        => await accountTypeGroupRepository.GetAccountTypeGroupByIdAsync(id, cancellationToken);

    public async Task<Result<AccountTypeGroup>> CreateAccountTypeGroupAsync(CreateAccountTypeGroupCommand command, CancellationToken cancellationToken = default)
    {
        AccountTypeGroup accountTypeGroup;

        try
        {
            accountTypeGroup = new AccountTypeGroup(command.Name);
        }
        catch (ArgumentException ex)
        {
            return Result<AccountTypeGroup>.Failure(ex.Message);
        }

        var result = await accountTypeGroupRepository.AddAccountTypeGroupAsync(accountTypeGroup, cancellationToken);

        if (!result.IsSuccess)
            return Result<AccountTypeGroup>.Failure(result.Error);

        accountTypeGroup.Id = result.Value;
        return Result<AccountTypeGroup>.Success(accountTypeGroup);
    }

    public async Task<Result> UpdateAccountTypeGroupAsync(UpdateAccountTypeGroupCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var accountTypeGroup = new AccountTypeGroup(command.Id, command.Name);
            return await accountTypeGroupRepository.UpdateAccountTypeGroupAsync(accountTypeGroup, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAccountTypeGroupAsync(int accountTypeGroupId, CancellationToken cancellationToken = default)
        => await accountTypeGroupRepository.DeleteAccountTypeGroupAsync(accountTypeGroupId, cancellationToken);

    public async Task<Result> ReorderAccountTypeGroupsAsync(ReorderAccountTypeGroupCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Reorder account type groups command cannot be null.");

        return await accountTypeGroupRepository.ReorderAccountTypeGroupsAsync(command.OrderedAccountTypeGroupIds, cancellationToken);
    }
}