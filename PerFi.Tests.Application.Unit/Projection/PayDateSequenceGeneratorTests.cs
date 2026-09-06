using PerFi.Application.Services.Projection;
using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Application.Unit.Projection;

public class PayDateSequenceGeneratorTests
{
    [Fact]
    public void AnnualPayCycleCount_ReturnsExpectedValuesForEachType()
    {
        Assert.Equal(52, PayDateSequenceGenerator.AnnualPayCycleCount(PayCycleType.Weekly));
        Assert.Equal(26, PayDateSequenceGenerator.AnnualPayCycleCount(PayCycleType.BiWeekly));
        Assert.Equal(24, PayDateSequenceGenerator.AnnualPayCycleCount(PayCycleType.SemiMonthly));
        Assert.Equal(12, PayDateSequenceGenerator.AnnualPayCycleCount(PayCycleType.Monthly));
    }

    [Fact]
    public void GeneratePayDates_WithRangeEndBeforeRangeStart_ReturnsEmpty()
    {
        var dates = PayDateSequenceGenerator.GeneratePayDates(
            PayCycleType.Weekly, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1));

        Assert.Empty(dates);
    }

    [Fact]
    public void GeneratePayDates_Weekly_ReturnsEveryReferenceIntervalWithinRange()
    {
        var reference = new DateOnly(2026, 1, 2);

        var dates = PayDateSequenceGenerator.GeneratePayDates(
            PayCycleType.Weekly, reference, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(
            [new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 9), new DateOnly(2026, 1, 16), new DateOnly(2026, 1, 23), new DateOnly(2026, 1, 30)],
            dates);
    }

    [Fact]
    public void GeneratePayDates_BiWeekly_ReturnsEveryOtherWeek()
    {
        var reference = new DateOnly(2026, 1, 2);

        var dates = PayDateSequenceGenerator.GeneratePayDates(
            PayCycleType.BiWeekly, reference, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 15));

        Assert.Equal([new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 16), new DateOnly(2026, 1, 30), new DateOnly(2026, 2, 13)], dates);
    }

    [Fact]
    public void GeneratePayDates_Monthly_ClampsToEndOfShorterMonths()
    {
        var reference = new DateOnly(2026, 1, 31);

        var dates = PayDateSequenceGenerator.GeneratePayDates(
            PayCycleType.Monthly, reference, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30));

        Assert.Equal(
            [new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 30)],
            dates);
    }

    [Fact]
    public void GeneratePayDates_SemiMonthly_ClampsSecondDateToMonthEnd()
    {
        // Reference day 20 -> day+15 = 35, which must clamp to the last day of each month.
        var reference = new DateOnly(2026, 1, 20);

        var dates = PayDateSequenceGenerator.GeneratePayDates(
            PayCycleType.SemiMonthly, reference, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        Assert.Equal([new DateOnly(2026, 2, 20), new DateOnly(2026, 2, 28)], dates);
    }

    [Fact]
    public void GeneratePayDates_SemiMonthly_ProducesTwoDatesPerMonthWhenNotClamped()
    {
        var reference = new DateOnly(2026, 1, 1);

        var dates = PayDateSequenceGenerator.GeneratePayDates(
            PayCycleType.SemiMonthly, reference, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        Assert.Equal([new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 16)], dates);
    }
}
