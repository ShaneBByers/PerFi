using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain.Unit;

public class UserConfigurationTests
{
    [Fact]
    public void CreatingUserConfiguration_SetsPropertiesCorrectly()
    {
        var birthDate = new DateOnly(1990, 5, 15);
        var referencePayDate = new DateOnly(2026, 1, 2);

        var config = new UserConfiguration(
            birthDate,
            PayCycleType.BiWeekly,
            referencePayDate,
            expectedAnnualSalaryRaisePercentage: 0.03m,
            expectedAnnualInflationPercentage: 0.025m);

        Assert.Equal(birthDate, config.BirthDate);
        Assert.Equal(PayCycleType.BiWeekly, config.PayCycleType);
        Assert.Equal(referencePayDate, config.ReferencePayDate);
        Assert.Equal(0.03m, config.ExpectedAnnualSalaryRaisePercentage);
        Assert.Equal(0.025m, config.ExpectedAnnualInflationPercentage);
    }

    [Fact]
    public void CreatingUserConfiguration_WithId_SetsId()
    {
        var birthDate = new DateOnly(1985, 3, 20);
        var referencePayDate = new DateOnly(2026, 1, 9);

        var config = new UserConfiguration(
            42,
            birthDate,
            PayCycleType.Weekly,
            referencePayDate,
            expectedAnnualSalaryRaisePercentage: 0.04m,
            expectedAnnualInflationPercentage: 0.03m);

        Assert.Equal(42, config.Id);
        Assert.Equal(birthDate, config.BirthDate);
        Assert.Equal(PayCycleType.Weekly, config.PayCycleType);
        Assert.Equal(referencePayDate, config.ReferencePayDate);
        Assert.Equal(0.04m, config.ExpectedAnnualSalaryRaisePercentage);
        Assert.Equal(0.03m, config.ExpectedAnnualInflationPercentage);
    }
}
