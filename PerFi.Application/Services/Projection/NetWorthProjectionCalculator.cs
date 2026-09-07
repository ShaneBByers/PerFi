using PerFi.Domain.Entities;
using PerFi.Domain.Entities.Projections;

namespace PerFi.Application.Services.Projection;

using PlansByAccountAndType = IReadOnlyDictionary<(int AccountId, ContributionContributorType ContributorType), IReadOnlyList<AccountContributionPlan>>;

internal static class NetWorthProjectionCalculator
{
    private const int RetirementAge = 65;

    public static IReadOnlyList<ProjectionPeriod> Calculate(
        IReadOnlyList<Account> accounts,
        IReadOnlyList<FinanceSnapshot> snapshots,
        IReadOnlyList<Contribution> contributions,
        IReadOnlyList<AccountContributionPlan> contributionPlans,
        UserConfiguration userConfiguration,
        IReadOnlyList<SalaryProgression> salaryProgressions,
        DateOnly today)
    {
        if (accounts.Count == 0)
            return [];

        var cutoffDate = new DateOnly(userConfiguration.BirthDate.AddYears(RetirementAge).Year, 12, 31);
        var earliestSnapshotDate = snapshots.Count > 0 ? snapshots.Min(snapshot => snapshot.Date) : today;
        var overallStart = snapshots.Count > 0 ? GetQuarterStart(earliestSnapshotDate) : new DateOnly(earliestSnapshotDate.Year, 1, 1);

        if (cutoffDate < overallStart)
            cutoffDate = overallStart;

        PlansByAccountAndType plansByAccountAndType = contributionPlans
            .GroupBy(plan => (plan.AccountId, plan.ContributorType))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<AccountContributionPlan>)group.ToList());

        var contributionsByAccount = contributions
            .GroupBy(contribution => contribution.AccountId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Contribution>)group.ToList());

        var checkpoints = BuildQuarterEndCheckpoints(overallStart, cutoffDate);

        var accountTimelines = accounts
            .Select(account => BuildAccountTimeline(
                account,
                snapshots,
                contributionsByAccount.TryGetValue(account.Id, out var accountContributions) ? accountContributions : [],
                plansByAccountAndType,
                userConfiguration,
                salaryProgressions,
                checkpoints,
                today))
            .ToList();

        var groupNames = accounts
            .Select(account => account.Type.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var periods = new List<ProjectionPeriod>();

        for (var index = 1; index < checkpoints.Count; index++)
        {
            var quarter = ((checkpoints[index].Month - 1) / 3) + 1;

            periods.Add(BuildPeriod(
                ProjectionPeriodType.Quarterly,
                checkpoints,
                index - 1,
                index,
                quarter,
                accountTimelines,
                groupNames,
                userConfiguration,
                salaryProgressions,
                plansByAccountAndType,
                today));

            if (checkpoints[index] is { Month: 12, Day: 31 })
            {
                periods.Add(BuildPeriod(
                    ProjectionPeriodType.Annual,
                    checkpoints,
                    Math.Max(0, index - 4),
                    index,
                    null,
                    accountTimelines,
                    groupNames,
                    userConfiguration,
                    salaryProgressions,
                    plansByAccountAndType,
                    today));
            }
        }

        return [.. periods.OrderBy(period => period.PeriodStart).ThenBy(period => period.PeriodType)];
    }

    private sealed record AccountTimeline(
        Account Account,
        IReadOnlyDictionary<DateOnly, decimal> BalanceAtCheckpoint,
        IReadOnlyDictionary<DateOnly, decimal> ContributedEndingAtCheckpoint,
        DateOnly? FirstSnapshotDate);

    private static DateOnly GetQuarterStart(DateOnly date)
    {
        var quarterStartMonth = (((date.Month - 1) / 3) * 3) + 1;
        return new DateOnly(date.Year, quarterStartMonth, 1);
    }

    private static List<DateOnly> BuildQuarterEndCheckpoints(DateOnly overallStart, DateOnly cutoffDate)
    {
        var checkpoints = new List<DateOnly> { overallStart };

        for (var year = overallStart.Year; year <= cutoffDate.Year; year++)
        {
            AddQuarterEndIfAfterStart(checkpoints, new DateOnly(year, 3, 31), overallStart);
            AddQuarterEndIfAfterStart(checkpoints, new DateOnly(year, 6, 30), overallStart);
            AddQuarterEndIfAfterStart(checkpoints, new DateOnly(year, 9, 30), overallStart);
            AddQuarterEndIfAfterStart(checkpoints, new DateOnly(year, 12, 31), overallStart);
        }

        return checkpoints;
    }

    // Skips any quarter-end at or before the anchor, so pre-first-snapshot quarters within the anchor's own year are never generated.
    private static void AddQuarterEndIfAfterStart(List<DateOnly> checkpoints, DateOnly quarterEnd, DateOnly overallStart)
    {
        if (quarterEnd > overallStart)
            checkpoints.Add(quarterEnd);
    }

    private static AccountTimeline BuildAccountTimeline(
        Account account,
        IReadOnlyList<FinanceSnapshot> snapshots,
        IReadOnlyList<Contribution> accountContributions,
        PlansByAccountAndType plansByAccountAndType,
        UserConfiguration userConfiguration,
        IReadOnlyList<SalaryProgression> salaryProgressions,
        IReadOnlyList<DateOnly> checkpoints,
        DateOnly today)
    {
        var historicalPoints = snapshots
            .Select(snapshot => (snapshot.Date, Balance: snapshot.AccountBalances.FirstOrDefault(balance => balance.Account.Id == account.Id)))
            .Where(entry => entry.Balance is not null)
            .Select(entry => (entry.Date, entry.Balance!.Balance))
            .OrderBy(entry => entry.Date)
            .ToList();

        var firstSnapshotDate = historicalPoints.Count > 0 ? historicalPoints[0].Date : (DateOnly?)null;

        decimal GetHistoricalBalance(DateOnly date) =>
            historicalPoints.Count == 0 ? 0m : LinearBalanceInterpolator.Interpolate(historicalPoints, date);

        decimal GetHistoricalContributed(DateOnly rangeStartExclusive, DateOnly rangeEndInclusive) =>
            accountContributions
                .Where(contribution => contribution.Date > rangeStartExclusive && contribution.Date <= rangeEndInclusive)
                .Sum(contribution => contribution.Amount);

        var balances = new Dictionary<DateOnly, decimal>();
        var contributedEndingAt = new Dictionary<DateOnly, decimal>();
        DateOnly? lastPastCheckpoint = null;

        for (var index = 0; index < checkpoints.Count; index++)
        {
            var checkpoint = checkpoints[index];
            if (checkpoint > today)
                break;

            balances[checkpoint] = GetHistoricalBalance(checkpoint);

            if (index > 0)
                contributedEndingAt[checkpoint] = GetHistoricalContributed(checkpoints[index - 1], checkpoint);

            lastPastCheckpoint = checkpoint;
        }

        var futureCheckpoints = checkpoints.Where(checkpoint => checkpoint > today).ToList();

        if (futureCheckpoints.Count > 0)
        {
            var payCycleType = userConfiguration.PayCycleType;
            var referenceDate = userConfiguration.ReferencePayDate;
            var annualPayCycles = PayDateSequenceGenerator.AnnualPayCycleCount(payCycleType);

            var payDates = PayDateSequenceGenerator.GeneratePayDates(payCycleType, referenceDate, today.AddDays(1), checkpoints[^1]);
            var payDateSet = payDates.ToHashSet();
            var checkpointSet = futureCheckpoints.ToHashSet();

            var events = payDates.Concat(futureCheckpoints).Distinct().OrderBy(date => date).ToList();

            var currentBalance = GetHistoricalBalance(today);
            var currentDate = today;

            // The first future checkpoint is the in-progress quarter's end: seed it with the actual
            // contributions already made this quarter (start-of-quarter to today) so they aren't
            // misattributed to Growth once the projected remainder of the quarter is simulated below.
            var contributedSinceLastCheckpoint = lastPastCheckpoint.HasValue
                ? GetHistoricalContributed(lastPastCheckpoint.Value, today)
                : 0m;

            foreach (var eventDate in events)
            {
                var elapsedDays = eventDate.DayNumber - currentDate.DayNumber;
                currentBalance = AccountGrowthSimulator.ApplyGrowth(currentBalance, elapsedDays, account.ExpectedAnnualGrowthPercentage);

                if (payDateSet.Contains(eventDate))
                {
                    var contribution = CalculateAccountContribution(
                        account.Id,
                        eventDate,
                        plansByAccountAndType,
                        userConfiguration,
                        salaryProgressions,
                        annualPayCycles);

                    currentBalance += contribution;
                    contributedSinceLastCheckpoint += contribution;
                }

                currentDate = eventDate;

                if (checkpointSet.Contains(eventDate))
                {
                    balances[eventDate] = currentBalance;
                    contributedEndingAt[eventDate] = contributedSinceLastCheckpoint;
                    contributedSinceLastCheckpoint = 0m;
                }
            }
        }

        return new AccountTimeline(account, balances, contributedEndingAt, firstSnapshotDate);
    }

    private static decimal CalculateAccountContribution(
        int accountId,
        DateOnly payDate,
        PlansByAccountAndType plansByAccountAndType,
        UserConfiguration userConfiguration,
        IReadOnlyList<SalaryProgression> salaryProgressions,
        int annualPayCycles)
    {
        var annualSalary = SalaryLookup.GetSalaryAt(salaryProgressions, userConfiguration.ExpectedAnnualSalaryRaisePercentage, payDate);
        var salaryPerPayCycle = annualSalary / annualPayCycles;
        var isAnnualLumpDate = IsFirstPayDateOfYear(userConfiguration.PayCycleType, userConfiguration.ReferencePayDate, payDate);

        var total = 0m;

        foreach (var contributorType in Enum.GetValues<ContributionContributorType>())
        {
            if (!plansByAccountAndType.TryGetValue((accountId, contributorType), out var rows))
                continue;

            var applicable = AccountContributionPlanLookup.FindApplicable(rows, payDate);
            total += ContributionPlanCalculator.CalculateContribution(applicable, payDate, isAnnualLumpDate, salaryPerPayCycle, annualSalary);
        }

        return total;
    }

    private static bool IsFirstPayDateOfYear(PayCycleType payCycleType, DateOnly referenceDate, DateOnly payDate)
    {
        var yearStart = new DateOnly(payDate.Year, 1, 1);
        var datesThisYearSoFar = PayDateSequenceGenerator.GeneratePayDates(payCycleType, referenceDate, yearStart, payDate);

        return datesThisYearSoFar.Count > 0 && datesThisYearSoFar.Min() == payDate;
    }

    private static ProjectionPeriod BuildPeriod(
        ProjectionPeriodType periodType,
        IReadOnlyList<DateOnly> checkpoints,
        int startIndex,
        int endIndex,
        int? quarter,
        IReadOnlyList<AccountTimeline> accountTimelines,
        IReadOnlyList<string> groupNames,
        UserConfiguration userConfiguration,
        IReadOnlyList<SalaryProgression> salaryProgressions,
        PlansByAccountAndType plansByAccountAndType,
        DateOnly today)
    {
        var startBoundaryDate = checkpoints[startIndex];
        var periodEnd = checkpoints[endIndex];
        var periodStart = startIndex == 0 ? checkpoints[0] : startBoundaryDate.AddDays(1);
        var isFullyHistorical = periodEnd <= today;

        var groups = groupNames
            .Select(groupName => BuildGroup(
                groupName,
                accountTimelines.Where(timeline => timeline.Account.Type.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).ToList(),
                checkpoints,
                startIndex,
                endIndex,
                startBoundaryDate,
                periodStart,
                periodEnd,
                isFullyHistorical,
                userConfiguration,
                salaryProgressions,
                plansByAccountAndType,
                today))
            .ToList();

        var total = new ProjectionGroup(
            "Total",
            Sum(groups.Select(group => group.Amounts)),
            Sum(groups.Select(group => group.AmountsInTodaysDollars)),
            groups.Any(group => group.ExpectedAmounts is not null)
                ? Sum(groups.Where(group => group.ExpectedAmounts is not null).Select(group => group.ExpectedAmounts!))
                : null,
            groups.Any(group => group.ExpectedAmountsInTodaysDollars is not null)
                ? Sum(groups.Where(group => group.ExpectedAmountsInTodaysDollars is not null).Select(group => group.ExpectedAmountsInTodaysDollars!))
                : null);

        var ageAtStart = CalculateAge(userConfiguration.BirthDate, periodStart);
        var ageAtEnd = CalculateAge(userConfiguration.BirthDate, periodEnd);

        return new ProjectionPeriod(
            periodType,
            periodStart,
            periodEnd,
            periodEnd.Year,
            quarter,
            !isFullyHistorical,
            ageAtStart,
            ageAtEnd,
            CalculateFractionalAge(userConfiguration.BirthDate, periodStart, periodEnd),
            SalaryLookup.GetSalaryAt(salaryProgressions, userConfiguration.ExpectedAnnualSalaryRaisePercentage, periodStart),
            SalaryLookup.GetSalaryAt(salaryProgressions, userConfiguration.ExpectedAnnualSalaryRaisePercentage, periodEnd),
            groups,
            total);
    }

    private static ProjectionGroup BuildGroup(
        string groupName,
        IReadOnlyList<AccountTimeline> timelines,
        IReadOnlyList<DateOnly> checkpoints,
        int startIndex,
        int endIndex,
        DateOnly startBoundaryDate,
        DateOnly periodStart,
        DateOnly periodEnd,
        bool isFullyHistorical,
        UserConfiguration userConfiguration,
        IReadOnlyList<SalaryProgression> salaryProgressions,
        PlansByAccountAndType plansByAccountAndType,
        DateOnly today)
    {
        var starting = 0m;
        var final = 0m;
        var contributed = 0m;

        var expectedStarting = 0m;
        var expectedContributed = 0m;
        var expectedFinal = 0m;
        var anyExpected = false;

        foreach (var timeline in timelines)
        {
            var accountStarting = timeline.BalanceAtCheckpoint.GetValueOrDefault(startBoundaryDate);
            var accountFinal = timeline.BalanceAtCheckpoint.GetValueOrDefault(periodEnd);

            var accountContributed = 0m;
            for (var j = startIndex + 1; j <= endIndex; j++)
                accountContributed += timeline.ContributedEndingAtCheckpoint.GetValueOrDefault(checkpoints[j]);

            starting += accountStarting;
            final += accountFinal;
            contributed += accountContributed;

            // Only backtest accounts that genuinely existed (had a real balance) as of the period's start.
            if (isFullyHistorical && timeline.FirstSnapshotDate is { } firstSnapshotDate && firstSnapshotDate <= startBoundaryDate)
            {
                var backtest = ComputeBacktest(
                    timeline.Account,
                    accountStarting,
                    periodStart,
                    periodEnd,
                    userConfiguration,
                    salaryProgressions,
                    plansByAccountAndType);

                expectedStarting += backtest.StartingAmount;
                expectedContributed += backtest.ContributedAmount;
                expectedFinal += backtest.FinalAmount;
                anyExpected = true;
            }
        }

        var amounts = new ProjectionAmounts(starting, contributed, final - starting - contributed, final);
        var amountsInTodaysDollars = InflationAdjuster.ToTodaysDollars(
            amounts, periodStart, periodEnd, userConfiguration.ExpectedAnnualInflationPercentage, today);

        ProjectionAmounts? expected = anyExpected
            ? new ProjectionAmounts(expectedStarting, expectedContributed, expectedFinal - expectedStarting - expectedContributed, expectedFinal)
            : null;

        var expectedInTodaysDollars = expected is null
            ? null
            : InflationAdjuster.ToTodaysDollars(expected, periodStart, periodEnd, userConfiguration.ExpectedAnnualInflationPercentage, today);

        return new ProjectionGroup(groupName, amounts, amountsInTodaysDollars, expected, expectedInTodaysDollars);
    }

    // Independent per-period simulation seeded from the period's real starting balance, using the plan row(s) actually in effect during this window.
    private static (decimal StartingAmount, decimal ContributedAmount, decimal FinalAmount) ComputeBacktest(
        Account account,
        decimal startingAmount,
        DateOnly periodStart,
        DateOnly periodEnd,
        UserConfiguration userConfiguration,
        IReadOnlyList<SalaryProgression> salaryProgressions,
        PlansByAccountAndType plansByAccountAndType)
    {
        var payCycleType = userConfiguration.PayCycleType;
        var referenceDate = userConfiguration.ReferencePayDate;
        var annualPayCycles = PayDateSequenceGenerator.AnnualPayCycleCount(payCycleType);
        var payDates = PayDateSequenceGenerator.GeneratePayDates(payCycleType, referenceDate, periodStart, periodEnd);

        var balance = startingAmount;
        var currentDate = periodStart;
        var contributed = 0m;

        foreach (var payDate in payDates)
        {
            var elapsedDays = payDate.DayNumber - currentDate.DayNumber;
            balance = AccountGrowthSimulator.ApplyGrowth(balance, elapsedDays, account.ExpectedAnnualGrowthPercentage);

            var contribution = CalculateAccountContribution(
                account.Id,
                payDate,
                plansByAccountAndType,
                userConfiguration,
                salaryProgressions,
                annualPayCycles);

            balance += contribution;
            contributed += contribution;
            currentDate = payDate;
        }

        var remainingDays = periodEnd.DayNumber - currentDate.DayNumber;
        balance = AccountGrowthSimulator.ApplyGrowth(balance, remainingDays, account.ExpectedAnnualGrowthPercentage);

        return (startingAmount, contributed, balance);
    }

    private static ProjectionAmounts Sum(IEnumerable<ProjectionAmounts> amounts)
    {
        var list = amounts.ToList();
        var starting = list.Sum(a => a.StartingAmount);
        var contributed = list.Sum(a => a.ContributedAmount);
        var final = list.Sum(a => a.FinalAmount);

        return new ProjectionAmounts(starting, contributed, final - starting - contributed, final);
    }

    private static int CalculateAge(DateOnly birthDate, DateOnly asOfDate)
    {
        var age = asOfDate.Year - birthDate.Year;
        if (asOfDate < birthDate.AddYears(age))
            age--;

        return age;
    }

    // Age at the period's midpoint expressed as a decimal (e.g. 34.5), rounded to the nearest quarter, so the UI shows a single intuitive age instead of a start-end range.
    private static decimal CalculateFractionalAge(DateOnly birthDate, DateOnly periodStart, DateOnly periodEnd)
    {
        var referenceDate = DateOnly.FromDayNumber((periodStart.DayNumber + periodEnd.DayNumber) / 2);

        var mostRecentBirthday = ToDateInYear(birthDate, referenceDate.Year);
        if (mostRecentBirthday > referenceDate)
            mostRecentBirthday = ToDateInYear(birthDate, referenceDate.Year - 1);

        var nextBirthday = ToDateInYear(birthDate, mostRecentBirthday.Year + 1);
        var ageAtMostRecentBirthday = mostRecentBirthday.Year - birthDate.Year;

        var fraction = (decimal)(referenceDate.DayNumber - mostRecentBirthday.DayNumber) / (nextBirthday.DayNumber - mostRecentBirthday.DayNumber);
        var roundedQuarter = Math.Round(fraction * 4, MidpointRounding.AwayFromZero) / 4m;

        if (roundedQuarter >= 1m)
        {
            roundedQuarter = 0m;
            ageAtMostRecentBirthday++;
        }

        return ageAtMostRecentBirthday + roundedQuarter;
    }

    private static DateOnly ToDateInYear(DateOnly birthDate, int year)
    {
        // Treat Feb 29 birthdays as Feb 28 in non-leap years.
        var day = birthDate is { Month: 2, Day: 29 } && !DateTime.IsLeapYear(year) ? 28 : birthDate.Day;
        return new DateOnly(year, birthDate.Month, day);
    }
}
