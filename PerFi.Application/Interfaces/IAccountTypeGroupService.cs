using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface IAccountTypeGroupService
{
    Task<IReadOnlyList<AccountTypeGroup>> GetAllAccountTypeGroupsAsync(CancellationToken cancellationToken = default);
    Task<AccountTypeGroup?> GetAccountTypeGroupByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AccountTypeGroup>> CreateAccountTypeGroupAsync(CreateAccountTypeGroupCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateAccountTypeGroupAsync(UpdateAccountTypeGroupCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteAccountTypeGroupAsync(int accountTypeGroupId, CancellationToken cancellationToken = default);
}