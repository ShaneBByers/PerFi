using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain.Unit;

public class UserConfigurationTests
{
    private static readonly UserConfigurationExpectations SampleExpectations = new(
        annualSalaryRaisePercentage: 0.03m,
        employerMatchPercentage: 0.05m,
        employerProfitSharingPercentage: 0.02m,
        annualBrokerageContributionPerPayCycleIncrease: 50m,
        annualStockMarketReturnPercentage: 0.08m,
        annualInflationPercentage: 0.025m,
        lastVerifiedDateTime: DateTimeOffset.UtcNow);

    [Fact]
    public void CreatingUserConfiguration_WithNullExpectations_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UserConfiguration(
            new DateOnly(1990, 1, 1),
            TimeSpan.FromDays(14),
            100_000m,
            DateTimeOffset.UtcNow,
            null!));
    }

    [Fact]
    public void CreatingUserConfiguration_SetsPropertiesCorrectly()
    {
        var birthDate = new DateOnly(1990, 5, 15);
        var payCycle = TimeSpan.FromDays(14);
        var salary = 120_000m;
        var verified = DateTimeOffset.UtcNow;

        var config = new UserConfiguration(birthDate, payCycle, salary, verified, SampleExpectations);

        Assert.Equal(birthDate, config.BirthDate);
        Assert.Equal(payCycle, config.PayCycle);
        Assert.Equal(salary, config.CurrentAnnualSalary);
        Assert.Equal(verified, config.LastVerifiedDateTime);
        Assert.Same(SampleExpectations, config.UserExpectations);
    }

    [Fact]
    public void CreatingUserConfiguration_WithId_SetsId()
    {
        var birthDate = new DateOnly(1985, 3, 20);
        var payCycle = TimeSpan.FromDays(7);
        var salary = 95_000m;
        var verified = DateTimeOffset.UtcNow;

        var config = new UserConfiguration(42, birthDate, payCycle, salary, verified, SampleExpectations);

        Assert.Equal(42, config.Id);
        Assert.Equal(birthDate, config.BirthDate);
        Assert.Equal(payCycle, config.PayCycle);
        Assert.Equal(salary, config.CurrentAnnualSalary);
        Assert.Equal(verified, config.LastVerifiedDateTime);
        Assert.Same(SampleExpectations, config.UserExpectations);
    }

    [Fact]
    public void CreatingUserConfigurationExpectations_SetsPropertiesCorrectly()
    {
        var verified = DateTimeOffset.UtcNow;
        var expectations = new UserConfigurationExpectations(
            id: 10,
            annualSalaryRaisePercentage: 0.04m,
            employerMatchPercentage: 0.06m,
            employerProfitSharingPercentage: 0.03m,
            annualBrokerageContributionPerPayCycleIncrease: 100m,
            annualStockMarketReturnPercentage: 0.09m,
            annualInflationPercentage: 0.03m,
            lastVerifiedDateTime: verified);

        Assert.Equal(10, expectations.Id);
        Assert.Equal(0.04m, expectations.AnnualSalaryRaisePercentage);
        Assert.Equal(0.06m, expectations.EmployerMatchPercentage);
        Assert.Equal(0.03m, expectations.EmployerProfitSharingPercentage);
        Assert.Equal(100m, expectations.AnnualBrokerageContributionPerPayCycleIncrease);
        Assert.Equal(0.09m, expectations.AnnualStockMarketReturnPercentage);
        Assert.Equal(0.03m, expectations.AnnualInflationPercentage);
        Assert.Equal(verified, expectations.LastVerifiedDateTime);
    }
}
