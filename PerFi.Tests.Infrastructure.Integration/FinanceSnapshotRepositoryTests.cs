using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class FinanceSnapshotRepositoryTests
{
    [Fact]
    public async Task UpdateSnapshotCellsAsync_WhenSaveFails_RollsBackAllChanges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PerFiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            await SeedSnapshotGraphAsync(setupContext);
        }

        await using (var throwingContext = new ThrowAfterSavePerFiDbContext(options))
        {
            var repository = new FinanceSnapshotRepository(throwingContext, new FakeCurrentUserService());

            var result = await repository.UpdateSnapshotCellsAsync(
                [new SnapshotCellUpdate(1, 1, 99m)]);

            Assert.True(result.IsFailure);
            Assert.Contains("rolled back", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        await using (var verifyContext = new PerFiDbContext(options))
        {
            var persistedBalance = await verifyContext.AccountBalances
                .Where(balance => balance.FinanceSnapshotId == 1 && balance.AccountId == 1)
                .Select(balance => balance.Balance)
                .SingleAsync();

            Assert.Equal(10m, persistedBalance);
        }
    }

    private static async Task SeedSnapshotGraphAsync(PerFiDbContext dbContext)
    {
        dbContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });

        var group = new AccountTypeGroupEntity
        {
            Id = 1,
            Name = "Assets",
            UserId = FakeCurrentUserService.DefaultUserId,
            AccountTypes = []
        };

        var type = new AccountTypeEntity
        {
            Id = 1,
            Name = "Checking",
            UserId = FakeCurrentUserService.DefaultUserId,
            AccountTypeGroupId = 1,
            AccountTypeGroup = group,
            Accounts = []
        };

        group.AccountTypes.Add(type);

        var institution = new InstitutionEntity
        {
            Id = 1,
            Name = "Test Bank",
            UserId = FakeCurrentUserService.DefaultUserId,
            Accounts = []
        };

        var account = new AccountEntity
        {
            Id = 1,
            Name = "Main Checking",
            UserId = FakeCurrentUserService.DefaultUserId,
            InstitutionId = 1,
            Institution = institution,
            AccountTypeId = 1,
            AccountType = type
        };

        institution.Accounts.Add(account);
        type.Accounts.Add(account);

        var snapshot = new FinanceSnapshotEntity
        {
            Id = 1,
            Date = new DateOnly(2026, 8, 9),
            UserId = FakeCurrentUserService.DefaultUserId,
            AccountBalances =
            [
                new AccountBalanceEntity
                {
                    Id = 1,
                    AccountId = 1,
                    Account = account,
                    FinanceSnapshotId = 1,
                    UserId = FakeCurrentUserService.DefaultUserId,
                    Balance = 10m
                }
            ]
        };

        dbContext.AccountTypeGroups.Add(group);
        dbContext.AccountTypes.Add(type);
        dbContext.Institutions.Add(institution);
        dbContext.Accounts.Add(account);
        dbContext.FinanceSnapshots.Add(snapshot);

        await dbContext.SaveChangesAsync();
    }

    private sealed class ThrowAfterSavePerFiDbContext(DbContextOptions<PerFiDbContext> options)
        : PerFiDbContext(options)
    {
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var rows = await base.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Simulated failure after save.");
        }
    }

    [Fact]
    public async Task GetSnapshotByIdAsync_WhenMissing_ReturnsNull()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new FinanceSnapshotRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetSnapshotByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddSnapshotAsync_ThenGetById_ReturnsCreatedSnapshot()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        int accountId = 0;
        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });

            var group = new AccountTypeGroupEntity { Name = "Assets", UserId = FakeCurrentUserService.DefaultUserId, AccountTypes = [] };
            var type = new AccountTypeEntity { Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, AccountTypeGroup = group, Accounts = [] };
            group.AccountTypes.Add(type);
            var institution = new InstitutionEntity { Name = "Test Bank", UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] };
            var account = new AccountEntity { Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = type };
            institution.Accounts.Add(account);

            setupContext.AccountTypeGroups.Add(group);
            setupContext.AccountTypes.Add(type);
            setupContext.Institutions.Add(institution);
            setupContext.Accounts.Add(account);
            await setupContext.SaveChangesAsync();
            accountId = account.Id;
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new FinanceSnapshotRepository(dbContext, new FakeCurrentUserService());

        var domainAccount = new PerFi.Domain.Entities.Account(accountId, "Checking", new PerFi.Domain.Entities.AccountType("Checking", new PerFi.Domain.Entities.AccountTypeGroup("Assets")));
        var snapshot = new PerFi.Domain.Entities.FinanceSnapshot(
            new DateOnly(2026, 8, 9),
            [new PerFi.Domain.Entities.AccountBalance(domainAccount, 250m)]);

        var result = await repository.AddSnapshotAsync(snapshot);

        Assert.True(result.IsSuccess);
        var created = await repository.GetSnapshotByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal(250m, created!.AccountBalances.Single().Balance);
    }

    [Fact]
    public async Task DeleteSnapshotAsync_WhenMissing_ReturnsFailure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new FinanceSnapshotRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteSnapshotAsync(99);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateSnapshotCellsAsync_WithValidUpdate_PersistsNewBalance()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            await SeedSnapshotGraphAsync(setupContext);
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new FinanceSnapshotRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateSnapshotCellsAsync([new PerFi.Domain.Entities.SnapshotCellUpdate(1, 1, 500m)]);

        Assert.True(result.IsSuccess);
        var updated = await repository.GetSnapshotByIdAsync(1);
        Assert.Equal(500m, updated!.AccountBalances.Single().Balance);
    }

    [Fact]
    public async Task UpdateSnapshotCellsAsync_WithMissingSnapshot_ReturnsFailure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            await SeedSnapshotGraphAsync(setupContext);
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new FinanceSnapshotRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateSnapshotCellsAsync([new PerFi.Domain.Entities.SnapshotCellUpdate(99, 1, 500m)]);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateSnapshotCellsAsync_WithMissingAccount_ReturnsFailure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            await SeedSnapshotGraphAsync(setupContext);
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new FinanceSnapshotRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateSnapshotCellsAsync([new PerFi.Domain.Entities.SnapshotCellUpdate(1, 99, 500m)]);

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public async Task AddSnapshotAsync_WithUnknownAccount_ReturnsFailure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new FinanceSnapshotRepository(dbContext, new FakeCurrentUserService());

        var domainAccount = new PerFi.Domain.Entities.Account(99, "Checking", new PerFi.Domain.Entities.AccountType("Checking", new PerFi.Domain.Entities.AccountTypeGroup("Assets")));
        var snapshot = new PerFi.Domain.Entities.FinanceSnapshot(
            new DateOnly(2026, 8, 9),
            [new PerFi.Domain.Entities.AccountBalance(domainAccount, 250m)]);

        var result = await repository.AddSnapshotAsync(snapshot);

        Assert.True(result.IsFailure);
    }
}
