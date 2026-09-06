using PerFi.Application.Interfaces;
using PerFi.Application.Services.Projection;
using PerFi.Domain.Entities.Projections;
using PerFi.Domain.Interfaces;

namespace PerFi.Application.Services;

internal class NetWorthProjectionService(
    IAccountRepository accountRepository,
    IFinanceSnapshotRepository financeSnapshotRepository,
    IContributionRepository contributionRepository,
    IAccountContributionPlanRepository accountContributionPlanRepository,
    ISalaryProgressionRepository salaryProgressionRepository,
    IUserConfigurationRepository userConfigurationRepository,
    TimeProvider timeProvider)
    : INetWorthProjectionService
{
    public async Task<IReadOnlyList<ProjectionPeriod>> GetProjectionAsync(CancellationToken cancellationToken = default)
    {
        var userConfiguration = await userConfigurationRepository.GetUserConfigurationAsync(cancellationToken);
        if (userConfiguration is null)
            return [];

        var accounts = await accountRepository.GetAllAccountsAsync(cancellationToken);
        if (accounts.Count == 0)
            return [];

        var snapshots = await financeSnapshotRepository.GetAllSnapshotsAsync(cancellationToken);
        var contributions = await contributionRepository.GetAllContributionsAsync(cancellationToken);
        var contributionPlans = await accountContributionPlanRepository.GetAllForCurrentUserAsync(cancellationToken);
        var salaryProgressions = await salaryProgressionRepository.GetAllSalaryProgressionsAsync(cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return NetWorthProjectionCalculator.Calculate(
            accounts,
            snapshots,
            contributions,
            contributionPlans,
            userConfiguration,
            salaryProgressions,
            today);
    }
}
