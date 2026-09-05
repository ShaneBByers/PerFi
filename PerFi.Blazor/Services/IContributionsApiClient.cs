using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface IContributionsApiClient
{
    Task<IReadOnlyList<ContributionResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ContributionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(DateOnly date, decimal amount, ContributionContributorType contributor, int accountId, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, DateOnly date, decimal amount, ContributionContributorType contributor, int accountId, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
