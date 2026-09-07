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

        // *AnnualIncrease percentages compound the whole per-cycle/annual contribution year over year (a raise on last year's contribution), not the salary percentage rate itself.
        var perPayCycleBase = applicablePlan.DollarAmountPerPayCycle
            + (applicablePlan.DollarAmountPerPayCycleAnnualIncrease * escalationYears)
            + (applicablePlan.PercentagePerPayCycle / 100m * salaryPerPayCycle);

        var contribution = perPayCycleBase * CompoundMultiplier(applicablePlan.PercentagePerPayCycleAnnualIncrease, escalationYears);

        if (isAnnualLumpDate)
        {
            var annualBase = applicablePlan.DollarAmountAnnual
                + (applicablePlan.DollarAmountAnnualIncrease * escalationYears)
                + (applicablePlan.PercentageAnnual / 100m * annualSalary);

            contribution += annualBase * CompoundMultiplier(applicablePlan.PercentageAnnualIncrease, escalationYears);
        }

        return contribution;
    }

    private static decimal CompoundMultiplier(decimal annualIncreasePercentage, int escalationYears)
    {
        var rate = 1m + (annualIncreasePercentage / 100m);
        var multiplier = 1m;

        for (var i = 0; i < escalationYears; i++)
            multiplier *= rate;

        return multiplier;
    }
}
