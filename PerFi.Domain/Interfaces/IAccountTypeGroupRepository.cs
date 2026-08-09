using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IAccountTypeGroupRepository
{
    Task<IReadOnlyList<AccountTypeGroup>> GetAllAccountTypeGroupsAsync(CancellationToken cancellationToken = default);
    Task<AccountTypeGroup?> GetAccountTypeGroupByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddAccountTypeGroupAsync(AccountTypeGroup accountTypeGroup, CancellationToken cancellationToken = default);
    Task<Result> UpdateAccountTypeGroupAsync(AccountTypeGroup accountTypeGroup, CancellationToken cancellationToken = default);
    Task<Result> DeleteAccountTypeGroupAsync(int accountTypeGroupId, CancellationToken cancellationToken = default);
    Task<Result> ReorderAccountTypeGroupsAsync(IReadOnlyList<int> orderedAccountTypeGroupIds, CancellationToken cancellationToken = default);
}