using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class SalaryProgressionRepositoryTests
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

    [Fact]
    public async Task GetAllSalaryProgressionsAsync_OnlyReturnsCurrentUsersEntries()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.SalaryProgressions.Add(new SalaryProgressionEntity
            {
                EffectiveDate = new DateOnly(2026, 1, 1),
                AnnualSalary = 100_000m,
                UserId = FakeCurrentUserService.DefaultUserId
            });

            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            dbContext.SalaryProgressions.Add(new SalaryProgressionEntity
            {
                EffectiveDate = new DateOnly(2026, 1, 1),
                AnnualSalary = 200_000m,
                UserId = "other-user"
            });

            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new SalaryProgressionRepository(dbContext, new FakeCurrentUserService());

        var results = await repository.GetAllSalaryProgressionsAsync();

        var entry = Assert.Single(results);
        Assert.Equal(100_000m, entry.AnnualSalary);
    }

    [Fact]
    public async Task AddSalaryProgressionAsync_WithDuplicateEffectiveDate_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new SalaryProgressionRepository(dbContext, new FakeCurrentUserService());

        var first = await repository.AddSalaryProgressionAsync(new SalaryProgression(new DateOnly(2026, 1, 1), 100_000m));
        Assert.True(first.IsSuccess);

        var second = await repository.AddSalaryProgressionAsync(new SalaryProgression(new DateOnly(2026, 1, 1), 110_000m));
        Assert.True(second.IsFailure);
        Assert.Contains("already exists", second.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddSalaryProgressionAsync_WithValidData_PersistsAndReturnsId()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new SalaryProgressionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddSalaryProgressionAsync(new SalaryProgression(new DateOnly(2026, 1, 1), 100_000m));

        Assert.True(result.IsSuccess);
        var created = await repository.GetSalaryProgressionByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal(100_000m, created!.AnnualSalary);
    }

    [Fact]
    public async Task UpdateSalaryProgressionAsync_WhenNotFound_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new SalaryProgressionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateSalaryProgressionAsync(new SalaryProgression(999, new DateOnly(2026, 1, 1), 100_000m));

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSalaryProgressionAsync_WhenFound_RemovesEntry()
    {
        var options = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new SalaryProgressionRepository(dbContext, new FakeCurrentUserService());

        var added = await repository.AddSalaryProgressionAsync(new SalaryProgression(new DateOnly(2026, 1, 1), 100_000m));

        var result = await repository.DeleteSalaryProgressionAsync(added.Value);

        Assert.True(result.IsSuccess);
        Assert.Null(await repository.GetSalaryProgressionByIdAsync(added.Value));
    }
}
