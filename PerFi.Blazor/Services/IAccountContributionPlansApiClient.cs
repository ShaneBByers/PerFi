using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface IAccountContributionPlansApiClient
{
    Task<IReadOnlyList<AccountContributionPlanResponse>> GetAllByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(int accountId, CreateAccountContributionPlanRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int accountId, int id, UpdateAccountContributionPlanRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int accountId, int id, CancellationToken cancellationToken = default);
}
