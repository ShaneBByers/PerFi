using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class UserConfigurationRepositoryTests
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

    private static UserConfiguration CreateSampleConfiguration(DateTimeOffset? verified = null)
    {
        var lastVerified = verified ?? DateTimeOffset.UtcNow;
        var expectations = new UserConfigurationExpectations(
            annualSalaryRaisePercentage: 0.03m,
            employerMatchPercentage: 0.05m,
            employerProfitSharingPercentage: 0.02m,
            annualBrokerageContributionPerPayCycleIncrease: 50m,
            annualStockMarketReturnPercentage: 0.08m,
            annualInflationPercentage: 0.025m,
            lastVerifiedDateTime: lastVerified);

        return new UserConfiguration(
            birthDate: new DateOnly(1990, 5, 15),
            payCycle: TimeSpan.FromDays(14),
            currentAnnualSalary: 120_000m,
            lastVerifiedDateTime: lastVerified,
            userExpectations: expectations);
    }

    [Fact]
    public async Task GetUserConfigurationAsync_WhenNoneExists_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetUserConfigurationAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task AddUserConfigurationAsync_PersistsConfigurationAndExpectations_AndReturnsId()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationRepository(dbContext, new FakeCurrentUserService());

        var config = CreateSampleConfiguration();
        var result = await repository.AddUserConfigurationAsync(config);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);

        var retrieved = await repository.GetUserConfigurationAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(config.BirthDate, retrieved.BirthDate);
        Assert.Equal(config.PayCycle, retrieved.PayCycle);
        Assert.Equal(config.CurrentAnnualSalary, retrieved.CurrentAnnualSalary);
        Assert.Equal(config.LastVerifiedDateTime, retrieved.LastVerifiedDateTime);
        Assert.NotNull(retrieved.UserExpectations);
        Assert.Equal(config.UserExpectations.AnnualSalaryRaisePercentage, retrieved.UserExpectations.AnnualSalaryRaisePercentage);
        Assert.Equal(config.UserExpectations.EmployerMatchPercentage, retrieved.UserExpectations.EmployerMatchPercentage);
        Assert.Equal(config.UserExpectations.EmployerProfitSharingPercentage, retrieved.UserExpectations.EmployerProfitSharingPercentage);
        Assert.Equal(config.UserExpectations.AnnualBrokerageContributionPerPayCycleIncrease, retrieved.UserExpectations.AnnualBrokerageContributionPerPayCycleIncrease);
        Assert.Equal(config.UserExpectations.AnnualStockMarketReturnPercentage, retrieved.UserExpectations.AnnualStockMarketReturnPercentage);
        Assert.Equal(config.UserExpectations.AnnualInflationPercentage, retrieved.UserExpectations.AnnualInflationPercentage);
    }

    [Fact]
    public async Task AddUserConfigurationAsync_WhenConfigurationAlreadyExistsForUser_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationRepository(dbContext, new FakeCurrentUserService());

        var first = await repository.AddUserConfigurationAsync(CreateSampleConfiguration());
        Assert.True(first.IsSuccess);

        var second = await repository.AddUserConfigurationAsync(CreateSampleConfiguration());
        Assert.True(second.IsFailure);
        Assert.Contains("already exists", second.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetUserConfigurationAsync_OnlyReturnsCurrentUsersConfiguration()
    {
        const string otherUserId = "other-user";
        var options = await CreateSeededOptionsAsync(async setupContext =>
        {
            setupContext.Users.Add(new ApplicationUser { Id = otherUserId, UserName = "other-user" });

            var otherConfig = new UserConfigurationEntity
            {
                BirthDate = new DateOnly(1980, 1, 1),
                PayCycle = TimeSpan.FromDays(7),
                CurrentAnnualSalary = 80_000m,
                LastVerifiedDateTime = DateTimeOffset.UtcNow,
                UserId = otherUserId,
                UserExpectations = new UserConfigurationExpectationsEntity
                {
                    AnnualSalaryRaisePercentage = 0.02m,
                    EmployerMatchPercentage = 0.03m,
                    EmployerProfitSharingPercentage = 0.01m,
                    AnnualBrokerageContributionPerPayCycleIncrease = 25m,
                    AnnualStockMarketReturnPercentage = 0.07m,
                    AnnualInflationPercentage = 0.02m,
                    LastVerifiedDateTime = DateTimeOffset.UtcNow,
                    UserId = otherUserId
                }
            };
            setupContext.UserConfigurations.Add(otherConfig);
            await setupContext.SaveChangesAsync();
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetUserConfigurationAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserConfigurationAsync_UpdatesPropertiesSuccessfully()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationRepository(dbContext, new FakeCurrentUserService());

        var initial = CreateSampleConfiguration();
        var addResult = await repository.AddUserConfigurationAsync(initial);
        Assert.True(addResult.IsSuccess);

        var newVerified = DateTimeOffset.UtcNow.AddDays(30);
        var updated = new UserConfiguration(
            id: addResult.Value,
            birthDate: new DateOnly(1991, 6, 20),
            payCycle: TimeSpan.FromDays(30),
            currentAnnualSalary: 135_000m,
            lastVerifiedDateTime: newVerified,
            userExpectations: initial.UserExpectations);

        var updateResult = await repository.UpdateUserConfigurationAsync(updated);
        Assert.True(updateResult.IsSuccess);

        var retrieved = await repository.GetUserConfigurationAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(new DateOnly(1991, 6, 20), retrieved.BirthDate);
        Assert.Equal(TimeSpan.FromDays(30), retrieved.PayCycle);
        Assert.Equal(135_000m, retrieved.CurrentAnnualSalary);
        Assert.Equal(newVerified, retrieved.LastVerifiedDateTime);
    }

    [Fact]
    public async Task UpdateUserConfigurationAsync_WhenNotFound_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new UserConfigurationRepository(dbContext, new FakeCurrentUserService());

        var config = new UserConfiguration(
            id: 999,
            birthDate: new DateOnly(1990, 1, 1),
            payCycle: TimeSpan.FromDays(14),
            currentAnnualSalary: 100_000m,
            lastVerifiedDateTime: DateTimeOffset.UtcNow,
            userExpectations: CreateSampleConfiguration().UserExpectations);

        var result = await repository.UpdateUserConfigurationAsync(config);
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
