using PerFi.Application.Services.Projection;
using PerFi.Domain.Entities;
using PerFi.Domain.Entities.Projections;
using Xunit;

namespace PerFi.Tests.Application.Unit.Projection;

public class NetWorthProjectionCalculatorTests
{
    private static readonly AccountTypeGroup Group = new(1, "Assets");
    private static readonly AccountType Type = new(1, "Brokerage", Group);

    private static Account CreateAccount(decimal expectedAnnualGrowthPercentage)
        => new(1, "Test Account", Type, 1) { ExpectedAnnualGrowthPercentage = expectedAnnualGrowthPercentage };

    private static FinanceSnapshot CreateSnapshot(Account account, DateOnly date, decimal balance)
        => new(date, [new AccountBalance(account, balance)]);

    private static UserConfiguration CreateUserConfiguration(
        PayCycleType payCycleType = PayCycleType.Monthly,
        DateOnly referencePayDate = default,
        decimal salaryRaisePercentage = 0m,
        decimal inflationPercentage = 0m,
        int retirementAge = 65)
        => new(new DateOnly(1990, 1, 1), payCycleType, referencePayDate == default ? new DateOnly(2026, 1, 1) : referencePayDate, salaryRaisePercentage, inflationPercentage, retirementAge);

    [Fact]
    public void Calculate_FutureYear_WithFlatDollarContributionPlanAndNoGrowth_MatchesHandComputedTotal()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 0m);
        var today = new DateOnly(2026, 1, 1);
        var snapshots = new List<FinanceSnapshot> { CreateSnapshot(account, today, 1000m) };
        var userConfiguration = CreateUserConfiguration(referencePayDate: today);

        var plan = new AccountContributionPlan(
            account.Id, ContributionContributorType.Self, today,
            dollarAmountPerPayCycle: 100m, dollarAmountPerPayCycleAnnualIncrease: 0m,
            dollarAmountAnnual: 0m, dollarAmountAnnualIncrease: 0m,
            percentagePerPayCycle: 0m, percentagePerPayCycleAnnualIncrease: 0m,
            percentageAnnual: 0m, percentageAnnualIncrease: 0m);

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], snapshots, [], [plan], userConfiguration, [], today);

        var year2026 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Annual && p.CalendarYear == 2026);

        Assert.True(year2026.IsProjected);
        // Feb-Dec (11 monthly payments after the Jan 1 seed date) x $100 = $1,100.
        Assert.Equal(1000m, year2026.Total.Amounts.StartingAmount);
        Assert.Equal(1100m, year2026.Total.Amounts.ContributedAmount);
        Assert.Equal(0m, year2026.Total.Amounts.GrowthAmount);
        Assert.Equal(2100m, year2026.Total.Amounts.FinalAmount);
    }

    [Fact]
    public void Calculate_FullyHistoricalPeriods_ReconcileGrowthAsResidualForAmountsAndTodaysDollars()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 10m);
        var today = new DateOnly(2026, 6, 1);
        var snapshots = new List<FinanceSnapshot>
        {
            CreateSnapshot(account, new DateOnly(2024, 1, 1), 1000m),
            CreateSnapshot(account, new DateOnly(2024, 12, 31), 1230m),
            CreateSnapshot(account, new DateOnly(2025, 12, 31), 1377.6m)
        };
        var userConfiguration = CreateUserConfiguration(inflationPercentage: 5m);

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], snapshots, [], [], userConfiguration, [], today);

        foreach (var period in periods)
        {
            foreach (var group in period.Groups.Append(period.Total))
            {
                AssertReconciles(group.Amounts);
                AssertReconciles(group.AmountsInTodaysDollars);

                if (group.ExpectedAmounts is not null)
                    AssertReconciles(group.ExpectedAmounts);

                if (group.ExpectedAmountsInTodaysDollars is not null)
                    AssertReconciles(group.ExpectedAmountsInTodaysDollars);
            }
        }

        static void AssertReconciles(ProjectionAmounts amounts)
        {
            var expectedGrowth = amounts.FinalAmount - amounts.StartingAmount - amounts.ContributedAmount;
            Assert.Equal(Math.Round(expectedGrowth, 6), Math.Round(amounts.GrowthAmount, 6));
        }
    }

    [Fact]
    public void Calculate_FullyHistoricalYear_ExpectedAmountsReflectFlatConfiguredGrowthNotRealGrowth()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 10m);
        var today = new DateOnly(2026, 1, 1);
        var snapshots = new List<FinanceSnapshot>
        {
            CreateSnapshot(account, new DateOnly(2024, 1, 1), 1000m),
            CreateSnapshot(account, new DateOnly(2024, 12, 31), 1230m) // 23% real growth.
        };
        var userConfiguration = CreateUserConfiguration();

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], snapshots, [], [], userConfiguration, [], today);

        var year2024 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Annual && p.CalendarYear == 2024);

        Assert.False(year2024.IsProjected);
        Assert.Equal(230m, year2024.Total.Amounts.GrowthAmount);
        Assert.NotNull(year2024.Total.ExpectedAmounts);
        Assert.Equal(100m, Math.Round(year2024.Total.ExpectedAmounts!.GrowthAmount, 2));
        Assert.NotEqual(year2024.Total.Amounts.GrowthAmount, year2024.Total.ExpectedAmounts.GrowthAmount);
    }

    [Fact]
    public void Calculate_StraddlingOrFuturePeriods_HaveNullExpectedAmounts()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 10m);
        var today = new DateOnly(2026, 1, 1);
        var snapshots = new List<FinanceSnapshot> { CreateSnapshot(account, new DateOnly(2024, 1, 1), 1000m) };
        var userConfiguration = CreateUserConfiguration();

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], snapshots, [], [], userConfiguration, [], today);

        var year2026 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Annual && p.CalendarYear == 2026);

        Assert.True(year2026.IsProjected);
        Assert.Null(year2026.Total.ExpectedAmounts);
        Assert.Null(year2026.Total.ExpectedAmountsInTodaysDollars);
    }

    [Fact]
    public void Calculate_ActualContributionMadeEarlierInTheCurrentInProgressQuarter_CountsAsContributedNotGrowth()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 0m);
        var quarterStart = new DateOnly(2026, 7, 1);
        var today = new DateOnly(2026, 9, 7); // Mid Q3 2026 (quarter ends 2026-09-30).
        var snapshots = new List<FinanceSnapshot>
        {
            CreateSnapshot(account, quarterStart, 1000m),
            CreateSnapshot(account, today, 1500m)
        };
        var contributions = new List<Contribution>
        {
            new(new DateOnly(2026, 8, 15), 500m, ContributionContributorType.Self, account.Id)
        };
        var userConfiguration = CreateUserConfiguration();

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], snapshots, contributions, [], userConfiguration, [], today);

        // The in-progress Q3 now splits into an actual (Jul 1 - Sep 7) and a projected (Sep 8 - Sep 30) period.
        var q3Periods = periods.Where(p => p.PeriodType == ProjectionPeriodType.Quarterly && p.CalendarYear == 2026 && p.Quarter == 3).ToList();
        Assert.Equal(2, q3Periods.Count);

        var actualQ3 = q3Periods.Single(p => !p.IsProjected);
        Assert.Equal(new DateOnly(2026, 9, 7), actualQ3.PeriodEnd);
        Assert.Equal(1000m, actualQ3.Total.Amounts.StartingAmount);
        Assert.Equal(500m, actualQ3.Total.Amounts.ContributedAmount);
        Assert.Equal(1500m, actualQ3.Total.Amounts.FinalAmount);
        Assert.Equal(0m, actualQ3.Total.Amounts.GrowthAmount);

        var projectedQ3 = q3Periods.Single(p => p.IsProjected);
        Assert.Equal(new DateOnly(2026, 9, 8), projectedQ3.PeriodStart);
        Assert.Equal(1500m, projectedQ3.Total.Amounts.StartingAmount);
        Assert.Equal(0m, projectedQ3.Total.Amounts.ContributedAmount);
        Assert.Equal(1500m, projectedQ3.Total.Amounts.FinalAmount);
        Assert.Equal(0m, projectedQ3.Total.Amounts.GrowthAmount);
    }

    [Fact]
    public void Calculate_InProgressQuarter_SplitsIntoActualAndProjectedWithoutDisturbingOtherQuarters()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 0m);
        var today = new DateOnly(2026, 8, 15); // Mid Q3 2026 (quarter ends 2026-09-30).
        var snapshots = new List<FinanceSnapshot>
        {
            CreateSnapshot(account, new DateOnly(2026, 1, 1), 1000m),
            CreateSnapshot(account, today, 1200m)
        };
        var userConfiguration = CreateUserConfiguration();

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], snapshots, [], [], userConfiguration, [], today);

        // Q1 and Q2 (fully historical, before the in-progress quarter) are untouched, single entries each.
        var q1 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Quarterly && p.CalendarYear == 2026 && p.Quarter == 1);
        Assert.False(q1.IsProjected);
        var q2 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Quarterly && p.CalendarYear == 2026 && p.Quarter == 2);
        Assert.False(q2.IsProjected);

        // Q3 splits into exactly 2 entries.
        var q3Periods = periods.Where(p => p.PeriodType == ProjectionPeriodType.Quarterly && p.CalendarYear == 2026 && p.Quarter == 3).ToList();
        Assert.Equal(2, q3Periods.Count);
        Assert.Contains(q3Periods, p => !p.IsProjected && p.PeriodEnd == today);
        Assert.Contains(q3Periods, p => p.IsProjected && p.PeriodStart == today.AddDays(1) && p.PeriodEnd == new DateOnly(2026, 9, 30));

        // Q4 (fully in the future) remains a single, fully-projected entry.
        var q4 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Quarterly && p.CalendarYear == 2026 && p.Quarter == 4);
        Assert.True(q4.IsProjected);

        // The Annual period for 2026 still spans Jan 1 - Dec 31 and its Contributed/Growth reconcile across the split quarters.
        var year2026 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Annual && p.CalendarYear == 2026);
        Assert.Equal(new DateOnly(2026, 1, 1), year2026.PeriodStart);
        Assert.Equal(new DateOnly(2026, 12, 31), year2026.PeriodEnd);
        Assert.Equal(1000m, year2026.Total.Amounts.StartingAmount);
    }

    [Fact]
    public void Calculate_HistoricalBacktest_UsesThePlanRowInEffectAtThatTimeNotANewerRow()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 0m);
        var today = new DateOnly(2026, 6, 1);
        var snapshots = new List<FinanceSnapshot> { CreateSnapshot(account, new DateOnly(2024, 1, 1), 1000m) };
        var userConfiguration = CreateUserConfiguration(referencePayDate: new DateOnly(2024, 1, 1));

        var earlyPlan = new AccountContributionPlan(
            account.Id, ContributionContributorType.Self, new DateOnly(2024, 1, 1),
            dollarAmountPerPayCycle: 100m, dollarAmountPerPayCycleAnnualIncrease: 10m,
            dollarAmountAnnual: 0m, dollarAmountAnnualIncrease: 0m,
            percentagePerPayCycle: 0m, percentagePerPayCycleAnnualIncrease: 0m,
            percentageAnnual: 0m, percentageAnnualIncrease: 0m);

        var laterPlan = new AccountContributionPlan(
            account.Id, ContributionContributorType.Self, new DateOnly(2026, 1, 1),
            dollarAmountPerPayCycle: 500m, dollarAmountPerPayCycleAnnualIncrease: 0m,
            dollarAmountAnnual: 0m, dollarAmountAnnualIncrease: 0m,
            percentagePerPayCycle: 0m, percentagePerPayCycleAnnualIncrease: 0m,
            percentageAnnual: 0m, percentageAnnualIncrease: 0m);

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], snapshots, [], [earlyPlan, laterPlan], userConfiguration, [], today);

        var year2025 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Annual && p.CalendarYear == 2025);

        // 12 monthly pay dates in 2025, escalated 1 year from the 2024 row's own EffectiveDate: (100 + 10*1) x 12 = 1320.
        Assert.NotNull(year2025.Total.ExpectedAmounts);
        Assert.Equal(1320m, year2025.Total.ExpectedAmounts!.ContributedAmount);
    }

    [Fact]
    public void Calculate_AccountsWithNoDataAtAll_ProduceNoErrorAndEmptyGroups()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 5m);
        var today = new DateOnly(2026, 1, 1);
        var userConfiguration = CreateUserConfiguration();

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], [], [], [], userConfiguration, [], today);

        Assert.NotEmpty(periods);
        Assert.All(periods, period => Assert.Equal(0m, period.Total.Amounts.StartingAmount));
    }

    [Fact]
    public void Calculate_FirstSnapshotMidYear_ProducesNoPeriodsBeforeTheFirstSnapshotsQuarter()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 0m);
        var today = new DateOnly(2026, 1, 1);
        var snapshots = new List<FinanceSnapshot> { CreateSnapshot(account, new DateOnly(2024, 4, 15), 1000m) };
        var userConfiguration = CreateUserConfiguration();

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], snapshots, [], [], userConfiguration, [], today);

        // Q1 2024 (Jan-Mar) is entirely before the first snapshot and must not appear.
        Assert.DoesNotContain(periods, p => p.PeriodType == ProjectionPeriodType.Quarterly && p.CalendarYear == 2024 && p.Quarter == 1);

        var q2 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Quarterly && p.CalendarYear == 2024 && p.Quarter == 2);
        Assert.Equal(new DateOnly(2024, 4, 1), q2.PeriodStart);

        // The 2024 annual period is scoped to only the available data (Apr-Dec), not a fabricated Jan-Dec range.
        var year2024 = Assert.Single(periods, p => p.PeriodType == ProjectionPeriodType.Annual && p.CalendarYear == 2024);
        Assert.Equal(new DateOnly(2024, 4, 1), year2024.PeriodStart);
    }

    [Fact]
    public void Calculate_UsesConfiguredRetirementAge_AsTheProjectionCutoff()
    {
        var account = CreateAccount(expectedAnnualGrowthPercentage: 0m);
        var today = new DateOnly(2026, 1, 1);
        // Birth date is 1990-01-01 (fixed in CreateUserConfiguration), so retirement age 36 -> cutoff year 2026.
        var userConfiguration = CreateUserConfiguration(retirementAge: 36);

        var periods = NetWorthProjectionCalculator.Calculate(
            [account], [], [], [], userConfiguration, [], today);

        Assert.Equal(2026, periods.Max(p => p.CalendarYear));
    }
}
