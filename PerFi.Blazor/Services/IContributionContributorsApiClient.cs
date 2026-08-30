using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface IContributionContributorsApiClient
{
    Task<IReadOnlyList<ContributionContributorResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ContributionContributorResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, string name, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedContributionContributorIds, CancellationToken cancellationToken = default);
}
