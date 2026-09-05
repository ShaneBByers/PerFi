using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class UserConfigurationExpectationsRepositoryTests
{
    private static async Task<DbContextOptions<PerFiDbContext>> CreateSeededOptionsAsync(Func<PerFiDbContext, Task>? seed = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using var setupContext = new PerFiDbContext(options);
        await setupContext.Database.EnsureCreatedAsync();
        setupContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });
        if (seed is not null)
        {
            await seed(setupContext);
        }
        await setupContext.SaveChangesAsync();

        return options;
    }

    private static UserConfigurationExpectations CreateSampleExpectations(DateTimeOffset? verified = null)
    {
        return new UserConfigurationExpectations(
            annualSalaryRaisePercentage: 0.03m,
            employerMatchPercentage: 0.05m,
            employerProfitSharingPercentage: 0.02m,
            annualBrokerageContributionPerPayCycleIncrease: 50m,
            annualStockMarketReturnPercentage: 0.08m,
            annualInflationPercentage: 0.025m,
            lastVerifiedDateTime: verified ?? DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GetUserConfigurationExpectationsAsync_WhenNoneExists_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationExpectationsRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetUserConfigurationExpectationsAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task AddUserConfigurationExpectationsAsync_WhenUserConfigurationDoesNotExist_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationExpectationsRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddUserConfigurationExpectationsAsync(CreateSampleExpectations());

        Assert.True(result.IsFailure);
        Assert.Contains("must exist", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddUserConfigurationExpectationsAsync_WhenExpectationsAlreadyExistForUser_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(async setupContext =>
        {
            var userConfigRepo = new UserConfigurationRepository(setupContext, new FakeCurrentUserService());
            await userConfigRepo.AddUserConfigurationAsync(new UserConfiguration(
                birthDate: new DateOnly(1990, 1, 1),
                payCycle: TimeSpan.FromDays(14),
                currentAnnualSalary: 100_000m,
                lastVerifiedDateTime: DateTimeOffset.UtcNow,
                userExpectations: CreateSampleExpectations()));
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationExpectationsRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddUserConfigurationExpectationsAsync(CreateSampleExpectations());

        Assert.True(result.IsFailure);
        Assert.Contains("already exist", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateUserConfigurationExpectationsAsync_WhenValid_UpdatesFields()
    {
        int expectationsId = 0;
        var options = await CreateSeededOptionsAsync(async setupContext =>
        {
            var userConfigRepo = new UserConfigurationRepository(setupContext, new FakeCurrentUserService());
            await userConfigRepo.AddUserConfigurationAsync(new UserConfiguration(
                birthDate: new DateOnly(1990, 1, 1),
                payCycle: TimeSpan.FromDays(14),
                currentAnnualSalary: 100_000m,
                lastVerifiedDateTime: DateTimeOffset.UtcNow,
                userExpectations: CreateSampleExpectations()));
            var savedExpectations = await setupContext.UserConfigurationExpectations.SingleAsync();
            expectationsId = savedExpectations.Id;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationExpectationsRepository(dbContext, new FakeCurrentUserService());

        var newVerified = DateTimeOffset.UtcNow.AddDays(10);
        var updated = new UserConfigurationExpectations(
            id: expectationsId,
            annualSalaryRaisePercentage: 0.05m,
            employerMatchPercentage: 0.06m,
            employerProfitSharingPercentage: 0.04m,
            annualBrokerageContributionPerPayCycleIncrease: 75m,
            annualStockMarketReturnPercentage: 0.10m,
            annualInflationPercentage: 0.03m,
            lastVerifiedDateTime: newVerified);

        var updateResult = await repository.UpdateUserConfigurationExpectationsAsync(updated);
        Assert.True(updateResult.IsSuccess);

        var retrieved = await repository.GetUserConfigurationExpectationsAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(0.05m, retrieved.AnnualSalaryRaisePercentage);
        Assert.Equal(0.06m, retrieved.EmployerMatchPercentage);
        Assert.Equal(0.04m, retrieved.EmployerProfitSharingPercentage);
        Assert.Equal(75m, retrieved.AnnualBrokerageContributionPerPayCycleIncrease);
        Assert.Equal(0.10m, retrieved.AnnualStockMarketReturnPercentage);
        Assert.Equal(0.03m, retrieved.AnnualInflationPercentage);
        Assert.Equal(newVerified, retrieved.LastVerifiedDateTime);
    }

    [Fact]
    public async Task UpdateUserConfigurationExpectationsAsync_WhenNotFound_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationExpectationsRepository(dbContext, new FakeCurrentUserService());

        var updated = new UserConfigurationExpectations(
            id: 999,
            annualSalaryRaisePercentage: 0.05m,
            employerMatchPercentage: 0.06m,
            employerProfitSharingPercentage: 0.04m,
            annualBrokerageContributionPerPayCycleIncrease: 75m,
            annualStockMarketReturnPercentage: 0.10m,
            annualInflationPercentage: 0.03m,
            lastVerifiedDateTime: DateTimeOffset.UtcNow);

        var result = await repository.UpdateUserConfigurationExpectationsAsync(updated);
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
