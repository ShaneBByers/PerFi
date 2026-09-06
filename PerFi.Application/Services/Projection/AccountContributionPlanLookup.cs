using PerFi.Domain.Entities;

namespace PerFi.Application.Services.Projection;

internal static class AccountContributionPlanLookup
{
    // rows should already be filtered to a single (AccountId, ContributorType) combination.
    public static AccountContributionPlan? FindApplicable(IReadOnlyList<AccountContributionPlan> rows, DateOnly targetDate)
    {
        AccountContributionPlan? applicable = null;

        foreach (var row in rows.OrderBy(r => r.EffectiveDate))
        {
            if (row.EffectiveDate > targetDate)
                break;
            applicable = row;
        }

        return applicable;
    }
}
