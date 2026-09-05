using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface IAccountContributionPlanService
{
    Task<IReadOnlyList<AccountContributionPlan>> GetAllByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<AccountContributionPlan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AccountContributionPlan>> CreateAsync(CreateAccountContributionPlanCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(UpdateAccountContributionPlanCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int accountContributionPlanId, CancellationToken cancellationToken = default);
}
