using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IAccountContributionPlanRepository
{
    Task<IReadOnlyList<AccountContributionPlan>> GetAllByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountContributionPlan>> GetAllForCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<AccountContributionPlan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddAsync(AccountContributionPlan plan, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(AccountContributionPlan plan, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
