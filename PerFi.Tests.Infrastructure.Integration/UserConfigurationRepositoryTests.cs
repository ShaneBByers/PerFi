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

    private static UserConfiguration CreateSampleConfiguration()
    {
        return new UserConfiguration(
            birthDate: new DateOnly(1990, 5, 15),
            payCycleType: PayCycleType.BiWeekly,
            referencePayDate: new DateOnly(2026, 1, 2),
            expectedAnnualSalaryRaisePercentage: 0.03m,
            expectedAnnualInflationPercentage: 0.025m,
            retirementAge: 65);
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
    public async Task AddUserConfigurationAsync_PersistsConfiguration_AndReturnsId()
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
        Assert.Equal(config.PayCycleType, retrieved.PayCycleType);
        Assert.Equal(config.ReferencePayDate, retrieved.ReferencePayDate);
        Assert.Equal(config.ExpectedAnnualSalaryRaisePercentage, retrieved.ExpectedAnnualSalaryRaisePercentage);
        Assert.Equal(config.ExpectedAnnualInflationPercentage, retrieved.ExpectedAnnualInflationPercentage);
        Assert.Equal(config.RetirementAge, retrieved.RetirementAge);
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
                PayCycleType = PayCycleType.Weekly,
                ReferencePayDate = new DateOnly(2026, 1, 2),
                ExpectedAnnualSalaryRaisePercentage = 0.02m,
                ExpectedAnnualInflationPercentage = 0.02m,
                RetirementAge = 65,
                UserId = otherUserId
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

        var updated = new UserConfiguration(
            id: addResult.Value,
            birthDate: new DateOnly(1991, 6, 20),
            payCycleType: PayCycleType.Monthly,
            referencePayDate: new DateOnly(2026, 2, 1),
            expectedAnnualSalaryRaisePercentage: 0.05m,
            expectedAnnualInflationPercentage: 0.03m,
            retirementAge: 70);

        var updateResult = await repository.UpdateUserConfigurationAsync(updated);
        Assert.True(updateResult.IsSuccess);

        var retrieved = await repository.GetUserConfigurationAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(new DateOnly(1991, 6, 20), retrieved.BirthDate);
        Assert.Equal(PayCycleType.Monthly, retrieved.PayCycleType);
        Assert.Equal(new DateOnly(2026, 2, 1), retrieved.ReferencePayDate);
        Assert.Equal(0.05m, retrieved.ExpectedAnnualSalaryRaisePercentage);
        Assert.Equal(0.03m, retrieved.ExpectedAnnualInflationPercentage);
        Assert.Equal(70, retrieved.RetirementAge);
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
            payCycleType: PayCycleType.BiWeekly,
            referencePayDate: new DateOnly(2026, 1, 2),
            expectedAnnualSalaryRaisePercentage: 0.03m,
            expectedAnnualInflationPercentage: 0.025m,
            retirementAge: 65);

        var result = await repository.UpdateUserConfigurationAsync(config);
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
