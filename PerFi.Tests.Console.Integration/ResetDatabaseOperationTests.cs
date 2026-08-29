using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Console.Operations;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using Xunit;

namespace PerFi.Tests.Console.Integration;

public sealed class ResetDatabaseOperationTests
{
    [Fact]
    public async Task ExecuteAsync_WithSkipConfirmation_DeletesAllFinancialDataButKeepsUsers()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.Users.Add(new ApplicationUser { Id = "user-1", UserName = "test-user" });

            var group = new AccountTypeGroupEntity { Name = "Assets", UserId = "user-1", AccountTypes = [] };
            var type = new AccountTypeEntity { Name = "Checking", UserId = "user-1", AccountTypeGroup = group, Accounts = [] };
            group.AccountTypes.Add(type);
            var institution = new InstitutionEntity { Name = "Bank", UserId = "user-1", Accounts = [] };
            var account = new AccountEntity { Name = "Checking", UserId = "user-1", Institution = institution, AccountType = type };
            institution.Accounts.Add(account);
            var snapshot = new FinanceSnapshotEntity
            {
                Date = new DateOnly(2026, 1, 1),
                UserId = "user-1",
                AccountBalances = [new AccountBalanceEntity { Account = account, UserId = "user-1", Balance = 10m }]
            };

            setupContext.AccountTypeGroups.Add(group);
            setupContext.AccountTypes.Add(type);
            setupContext.Institutions.Add(institution);
            setupContext.Accounts.Add(account);
            setupContext.FinanceSnapshots.Add(snapshot);
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new PerFiDbContext(options);
        var operation = new ResetDatabaseOperation(dbContext);

        await operation.ExecuteAsync(skipConfirmation: true);

        Assert.Empty(await dbContext.AccountBalances.ToListAsync());
        Assert.Empty(await dbContext.FinanceSnapshots.ToListAsync());
        Assert.Empty(await dbContext.Accounts.ToListAsync());
        Assert.Empty(await dbContext.Institutions.ToListAsync());
        Assert.Empty(await dbContext.AccountTypes.ToListAsync());
        Assert.Empty(await dbContext.AccountTypeGroups.ToListAsync());
        Assert.Single(await dbContext.Users.ToListAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSkipConfirmationAndWrongInput_AbortsWithoutDeleting()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.Users.Add(new ApplicationUser { Id = "user-1", UserName = "test-user" });
            setupContext.Institutions.Add(new InstitutionEntity { Name = "Bank", UserId = "user-1", Accounts = [] });
            await setupContext.SaveChangesAsync();
        }

        var originalIn = System.Console.In;
        System.Console.SetIn(new StringReader("nope"));
        try
        {
            await using var dbContext = new PerFiDbContext(options);
            var operation = new ResetDatabaseOperation(dbContext);

            await operation.ExecuteAsync(skipConfirmation: false);

            Assert.Single(await dbContext.Institutions.ToListAsync());
        }
        finally
        {
            System.Console.SetIn(originalIn);
        }
    }
}
