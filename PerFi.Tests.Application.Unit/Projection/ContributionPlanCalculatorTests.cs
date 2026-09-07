using PerFi.Application.Services.Projection;
using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Application.Unit.Projection;

public class ContributionPlanCalculatorTests
{
    [Fact]
    public void CalculateContribution_WithNullPlan_ReturnsZero()
    {
        var result = ContributionPlanCalculator.CalculateContribution(null, new DateOnly(2026, 1, 1), false, 1000m, 12000m);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateContribution_PerPayCycleDollarAndPercentage_Combine()
    {
        var plan = new AccountContributionPlan(
            1, ContributionContributorType.Self, new DateOnly(2026, 1, 1),
            dollarAmountPerPayCycle: 50m, dollarAmountPerPayCycleAnnualIncrease: 0m,
            dollarAmountAnnual: 0m, dollarAmountAnnualIncrease: 0m,
            percentagePerPayCycle: 5m, percentagePerPayCycleAnnualIncrease: 0m,
            percentageAnnual: 0m, percentageAnnualIncrease: 0m);

        // $50 flat + 5% of $1000 per-cycle salary = $50 + $50 = $100.
        var result = ContributionPlanCalculator.CalculateContribution(plan, new DateOnly(2026, 3, 1), false, 1000m, 12000m);

        Assert.Equal(100m, result);
    }

    [Fact]
    public void CalculateContribution_EscalatesFromThePlanRowsOwnEffectiveDate()
    {
        var plan = new AccountContributionPlan(
            1, ContributionContributorType.Self, new DateOnly(2024, 1, 1),
            dollarAmountPerPayCycle: 100m, dollarAmountPerPayCycleAnnualIncrease: 10m,
            dollarAmountAnnual: 0m, dollarAmountAnnualIncrease: 0m,
            percentagePerPayCycle: 0m, percentagePerPayCycleAnnualIncrease: 0m,
            percentageAnnual: 0m, percentageAnnualIncrease: 0m);

        // 2026 is 2 years after the plan's 2024 EffectiveDate: 100 + (10 * 2) = 120.
        var result = ContributionPlanCalculator.CalculateContribution(plan, new DateOnly(2026, 6, 1), false, 0m, 0m);

        Assert.Equal(120m, result);
    }

    [Fact]
    public void CalculateContribution_AnnualLumpOnlyAppliesOnLumpDate()
    {
        var plan = new AccountContributionPlan(
            1, ContributionContributorType.Self, new DateOnly(2026, 1, 1),
            dollarAmountPerPayCycle: 0m, dollarAmountPerPayCycleAnnualIncrease: 0m,
            dollarAmountAnnual: 1000m, dollarAmountAnnualIncrease: 0m,
            percentagePerPayCycle: 0m, percentagePerPayCycleAnnualIncrease: 0m,
            percentageAnnual: 2m, percentageAnnualIncrease: 0m);

        var nonLumpResult = ContributionPlanCalculator.CalculateContribution(plan, new DateOnly(2026, 6, 1), false, 0m, 100000m);
        var lumpResult = ContributionPlanCalculator.CalculateContribution(plan, new DateOnly(2026, 1, 15), true, 0m, 100000m);

        Assert.Equal(0m, nonLumpResult);
        // $1000 flat + 2% of $100,000 = $1000 + $2000 = $3000.
        Assert.Equal(3000m, lumpResult);
    }

    [Fact]
    public void CalculateContribution_PercentagePerPayCycleAnnualIncrease_CompoundsPriorYearContribution()
    {
        var plan = new AccountContributionPlan(
            1, ContributionContributorType.Self, new DateOnly(2026, 1, 1),
            dollarAmountPerPayCycle: 100m, dollarAmountPerPayCycleAnnualIncrease: 0m,
            dollarAmountAnnual: 0m, dollarAmountAnnualIncrease: 0m,
            percentagePerPayCycle: 5m, percentagePerPayCycleAnnualIncrease: 5m,
            percentageAnnual: 0m, percentageAnnualIncrease: 0m);

        // Year 1 (effective year, no escalation yet): $100 + 5% of $100,000 = $100 + $5,000 = $5,100.
        var year1 = ContributionPlanCalculator.CalculateContribution(plan, new DateOnly(2026, 3, 1), false, 100000m, 1200000m);

        // Year 2 (1 year escalated, salary already raised to $110,000 by the caller): ($100 + 5% of $110,000) * 1.05 = $5,600 * 1.05 = $5,880.
        var year2 = ContributionPlanCalculator.CalculateContribution(plan, new DateOnly(2027, 3, 1), false, 110000m, 1320000m);

        Assert.Equal(5100m, year1);
        Assert.Equal(5880m, year2);
    }
}
