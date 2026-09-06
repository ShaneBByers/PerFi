using PerFi.Domain.Entities;

namespace PerFi.Application.Services.Projection;

internal static class ContributionPlanCalculator
{
    // salaryPerPayCycle/annualSalary should come from SalaryLookup at payDate; isAnnualLumpDate marks that year's first pay date on/after Jan 1.
    public static decimal CalculateContribution(
        AccountContributionPlan? applicablePlan,
        DateOnly payDate,
        bool isAnnualLumpDate,
        decimal salaryPerPayCycle,
        decimal annualSalary)
    {
        if (applicablePlan is null)
            return 0m;

        // Always >= 0: the lookup guarantees applicablePlan.EffectiveDate <= payDate.
        var escalationYears = payDate.Year - applicablePlan.EffectiveDate.Year;

        var perPayCycleDollar = applicablePlan.DollarAmountPerPayCycle
            + (applicablePlan.DollarAmountPerPayCycleAnnualIncrease * escalationYears);

        var perPayCyclePercentage = applicablePlan.PercentagePerPayCycle
            + (applicablePlan.PercentagePerPayCycleAnnualIncrease * escalationYears);

        var contribution = perPayCycleDollar + (perPayCyclePercentage / 100m * salaryPerPayCycle);

        if (isAnnualLumpDate)
        {
            var annualDollar = applicablePlan.DollarAmountAnnual
                + (applicablePlan.DollarAmountAnnualIncrease * escalationYears);

            var annualPercentage = applicablePlan.PercentageAnnual
                + (applicablePlan.PercentageAnnualIncrease * escalationYears);

            contribution += annualDollar + (annualPercentage / 100m * annualSalary);
        }

        return contribution;
    }
}
