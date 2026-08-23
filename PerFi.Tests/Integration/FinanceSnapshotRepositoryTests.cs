using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using Xunit;

namespace PerFi.Tests.Integration;

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
}
