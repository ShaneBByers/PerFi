using PerFi.Application.Services.Projection;
using Xunit;

namespace PerFi.Tests.Application.Unit.Projection;

public class AccountGrowthSimulatorTests
{
    [Fact]
    public void ApplyGrowth_WithZeroDays_ReturnsBalanceUnchanged()
    {
        var result = AccountGrowthSimulator.ApplyGrowth(1000m, 0, 10m);

        Assert.Equal(1000m, result);
    }

    [Fact]
    public void ApplyGrowth_WithNegativeDays_ReturnsBalanceUnchanged()
    {
        var result = AccountGrowthSimulator.ApplyGrowth(1000m, -5, 10m);

        Assert.Equal(1000m, result);
    }

    [Fact]
    public void ApplyGrowth_OverFullYear_AppliesAnnualRate()
    {
        var result = AccountGrowthSimulator.ApplyGrowth(1000m, 365, 10m);

        Assert.Equal(1100.00m, Math.Round(result, 2));
    }

    [Fact]
    public void ApplyGrowth_OverHalfYear_AppliesRoughlyHalfTheAnnualEffect()
    {
        var result = AccountGrowthSimulator.ApplyGrowth(1000m, 182, 10m);

        Assert.True(result > 1040m && result < 1060m);
    }
}
