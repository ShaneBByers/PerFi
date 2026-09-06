using PerFi.Application.Services.Projection;
using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Application.Unit.Projection;

public class AccountContributionPlanLookupTests
{
    [Fact]
    public void FindApplicable_WithNoRows_ReturnsNull()
    {
        var result = AccountContributionPlanLookup.FindApplicable([], new DateOnly(2026, 1, 1));

        Assert.Null(result);
    }

    [Fact]
    public void FindApplicable_BeforeEarliestRow_ReturnsNull()
    {
        List<AccountContributionPlan> rows = [CreatePlan(new DateOnly(2025, 1, 1))];

        var result = AccountContributionPlanLookup.FindApplicable(rows, new DateOnly(2024, 1, 1));

        Assert.Null(result);
    }

    [Fact]
    public void FindApplicable_WithMultipleRows_ReturnsLatestApplicable()
    {
        var earlier = CreatePlan(new DateOnly(2024, 1, 1));
        var later = CreatePlan(new DateOnly(2026, 1, 1));
        List<AccountContributionPlan> rows = [earlier, later];

        var result = AccountContributionPlanLookup.FindApplicable(rows, new DateOnly(2025, 6, 1));

        Assert.Same(earlier, result);
    }

    [Fact]
    public void FindApplicable_OnOrAfterLatestRow_ReturnsLatestRow()
    {
        var earlier = CreatePlan(new DateOnly(2024, 1, 1));
        var later = CreatePlan(new DateOnly(2026, 1, 1));
        List<AccountContributionPlan> rows = [earlier, later];

        var result = AccountContributionPlanLookup.FindApplicable(rows, new DateOnly(2027, 1, 1));

        Assert.Same(later, result);
    }

    private static AccountContributionPlan CreatePlan(DateOnly effectiveDate)
        => new(1, ContributionContributorType.Self, effectiveDate, 100m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
}
