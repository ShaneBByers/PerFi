using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface IAccountTypeGroupsApiClient
{
    Task<IReadOnlyList<AccountTypeGroupResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AccountTypeGroupResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, string name, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedAccountTypeGroupIds, CancellationToken cancellationToken = default);
}