using PerFi.Application.Services.Projection;
using Xunit;

namespace PerFi.Tests.Application.Unit.Projection;

public class LinearBalanceInterpolatorTests
{
    [Fact]
    public void Interpolate_WithSinglePoint_ClampsToThatPointEverywhere()
    {
        List<(DateOnly Date, decimal Balance)> points = [(new DateOnly(2026, 1, 1), 100m)];

        Assert.Equal(100m, LinearBalanceInterpolator.Interpolate(points, new DateOnly(2020, 1, 1)));
        Assert.Equal(100m, LinearBalanceInterpolator.Interpolate(points, new DateOnly(2026, 1, 1)));
        Assert.Equal(100m, LinearBalanceInterpolator.Interpolate(points, new DateOnly(2030, 1, 1)));
    }

    [Fact]
    public void Interpolate_BeforeEarliestPoint_ClampsFlat()
    {
        List<(DateOnly Date, decimal Balance)> points = [(new DateOnly(2026, 1, 1), 100m), (new DateOnly(2026, 2, 1), 200m)];

        Assert.Equal(100m, LinearBalanceInterpolator.Interpolate(points, new DateOnly(2025, 1, 1)));
    }

    [Fact]
    public void Interpolate_AfterLatestPoint_ClampsFlat()
    {
        List<(DateOnly Date, decimal Balance)> points = [(new DateOnly(2026, 1, 1), 100m), (new DateOnly(2026, 2, 1), 200m)];

        Assert.Equal(200m, LinearBalanceInterpolator.Interpolate(points, new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Interpolate_AtMidpointBetweenTwoPoints_ReturnsAverage()
    {
        // Jan 1 -> Jan 11 is 10 days; Jan 6 is exactly halfway.
        List<(DateOnly Date, decimal Balance)> points = [(new DateOnly(2026, 1, 1), 100m), (new DateOnly(2026, 1, 11), 200m)];

        Assert.Equal(150m, LinearBalanceInterpolator.Interpolate(points, new DateOnly(2026, 1, 6)));
    }

    [Fact]
    public void Interpolate_ExactlyOnKnownPoint_ReturnsThatValue()
    {
        List<(DateOnly Date, decimal Balance)> points = [(new DateOnly(2026, 1, 1), 100m), (new DateOnly(2026, 2, 1), 200m), (new DateOnly(2026, 3, 1), 300m)];

        Assert.Equal(200m, LinearBalanceInterpolator.Interpolate(points, new DateOnly(2026, 2, 1)));
    }
}
