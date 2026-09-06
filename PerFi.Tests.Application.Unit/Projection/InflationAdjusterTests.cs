using PerFi.Application.Services.Projection;
using PerFi.Domain.Entities.Projections;
using Xunit;

namespace PerFi.Tests.Application.Unit.Projection;

public class InflationAdjusterTests
{
    [Fact]
    public void ToTodaysDollars_ForPastPeriod_InflatesAmountsUp()
    {
        var amounts = new ProjectionAmounts(1000m, 100m, 50m, 1150m);

        var result = InflationAdjuster.ToTodaysDollars(
            amounts, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), 10m, new DateOnly(2026, 1, 1));

        Assert.True(result.FinalAmount > amounts.FinalAmount);
        Assert.True(result.StartingAmount > amounts.StartingAmount);
    }

    [Fact]
    public void ToTodaysDollars_ForFuturePeriod_DiscountsAmountsDown()
    {
        var amounts = new ProjectionAmounts(1000m, 100m, 50m, 1150m);

        var result = InflationAdjuster.ToTodaysDollars(
            amounts, new DateOnly(2030, 1, 1), new DateOnly(2030, 12, 31), 10m, new DateOnly(2026, 1, 1));

        Assert.True(result.FinalAmount < amounts.FinalAmount);
        Assert.True(result.StartingAmount < amounts.StartingAmount);
    }

    [Fact]
    public void ToTodaysDollars_UsesDifferentReferenceDatesForStartingVersusOthers()
    {
        var amounts = new ProjectionAmounts(1000m, 0m, 0m, 1000m);

        var result = InflationAdjuster.ToTodaysDollars(
            amounts, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), 10m, new DateOnly(2026, 1, 1));

        // StartingAmount is referenced to the earlier PeriodStart (2 years before today), so it compounds
        // more than FinalAmount, which is referenced to the later PeriodEnd (~1 year before today).
        Assert.True(result.StartingAmount > result.FinalAmount);
    }

    [Fact]
    public void ToTodaysDollars_WithZeroInflation_ReturnsSameAmounts()
    {
        var amounts = new ProjectionAmounts(1000m, 100m, 50m, 1150m);

        var result = InflationAdjuster.ToTodaysDollars(
            amounts, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), 0m, new DateOnly(2026, 1, 1));

        Assert.Equal(amounts.StartingAmount, Math.Round(result.StartingAmount, 8));
        Assert.Equal(amounts.FinalAmount, Math.Round(result.FinalAmount, 8));
    }
}
