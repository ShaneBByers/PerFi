using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class AccountRepositoryTests
{
    private static async Task<DbContextOptions<PerFiDbContext>> CreateSeededOptionsAsync(Func<PerFiDbContext, Task> seed)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using var setupContext = new PerFiDbContext(options);
        await setupContext.Database.EnsureCreatedAsync();
        setupContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });
        await seed(setupContext);
        await setupContext.SaveChangesAsync();

        return options;
    }

    private static (AccountTypeGroupEntity Group, AccountTypeEntity Type, InstitutionEntity Institution) SeedBaseGraph(PerFiDbContext dbContext, string userId = FakeCurrentUserService.DefaultUserId)
    {
        var group = new AccountTypeGroupEntity { Name = "Assets", UserId = userId, AccountTypes = [] };
        var type = new AccountTypeEntity { Name = "Checking", UserId = userId, AccountTypeGroup = group, Accounts = [] };
        group.AccountTypes.Add(type);
        var institution = new InstitutionEntity { Name = "First Bank", UserId = userId, Accounts = [] };

        dbContext.AccountTypeGroups.Add(group);
        dbContext.AccountTypes.Add(type);
        dbContext.Institutions.Add(institution);

        return (group, type, institution);
    }

    [Fact]
    public async Task GetAllAccountsAsync_OnlyReturnsCurrentUsersAccounts()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (_, type, institution) = SeedBaseGraph(dbContext);
            institution.Accounts.Add(new AccountEntity { Name = "Mine", UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = type });

            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            var (_, otherType, otherInstitution) = SeedBaseGraph(dbContext, "other-user");
            otherInstitution.Accounts.Add(new AccountEntity { Name = "TheirsToo", UserId = "other-user", Institution = otherInstitution, AccountType = otherType });

            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountRepository(dbContext, new FakeCurrentUserService());

        var accounts = await repository.GetAllAccountsAsync();

        var account = Assert.Single(accounts);
        Assert.Equal("Mine", account.Name);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ForOtherUser_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            var (_, type, institution) = SeedBaseGraph(dbContext, "other-user");
            institution.Accounts.Add(new AccountEntity { Id = 1, Name = "Theirs", UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = type });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountRepository(dbContext, new FakeCurrentUserService());

        var account = await repository.GetAccountByIdAsync(1);

        Assert.Null(account);
    }

    [Fact]
    public async Task AddAccountAsync_WithMissingInstitution_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext => { SeedBaseGraph(dbContext); return Task.CompletedTask; });
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountRepository(dbContext, new FakeCurrentUserService());

        var accountType = new PerFi.Domain.Entities.AccountType(1, "Checking", new PerFi.Domain.Entities.AccountTypeGroup(1, "Assets"));
        var result = await repository.AddAccountAsync(new PerFi.Domain.Entities.Account("Checking", accountType), 99);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddAccountAsync_WithValidData_AssignsNextDisplayOrder()
    {
        int institutionId = 0, accountTypeId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (_, type, institution) = SeedBaseGraph(dbContext);
            institution.Accounts.Add(new AccountEntity { Name = "Existing", DisplayOrder = 1, UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = type });
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            institutionId = institution.Id;
            accountTypeId = type.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountRepository(dbContext, new FakeCurrentUserService());

        var accountType = new PerFi.Domain.Entities.AccountType(accountTypeId, "Checking", new PerFi.Domain.Entities.AccountTypeGroup(1, "Assets"));
        var result = await repository.AddAccountAsync(new PerFi.Domain.Entities.Account("New Account", accountType), institutionId);

        Assert.True(result.IsSuccess);
        var created = await repository.GetAccountByIdAsync(result.Value);
        Assert.Equal(2, created!.DisplayOrder);
    }

    [Fact]
    public async Task UpdateAccountAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext => { SeedBaseGraph(dbContext); return Task.CompletedTask; });
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountRepository(dbContext, new FakeCurrentUserService());

        var accountType = new PerFi.Domain.Entities.AccountType(1, "Checking", new PerFi.Domain.Entities.AccountTypeGroup(1, "Assets"));
        var result = await repository.UpdateAccountAsync(new PerFi.Domain.Entities.Account(99, "Checking", accountType), 1);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenReferencedByBalance_ReturnsFailure()
    {
        int accountId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (_, type, institution) = SeedBaseGraph(dbContext);
            var account = new AccountEntity { Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = type };
            institution.Accounts.Add(account);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            accountId = account.Id;

            dbContext.FinanceSnapshots.Add(new FinanceSnapshotEntity
            {
                Date = new DateOnly(2026, 1, 1),
                UserId = FakeCurrentUserService.DefaultUserId,
                AccountBalances = [new AccountBalanceEntity { AccountId = accountId, Account = account, UserId = FakeCurrentUserService.DefaultUserId, Balance = 5m }]
            });

            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteAccountAsync(accountId);

        Assert.True(result.IsFailure);
        Assert.Contains("referenced", result.Error);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithNoReferences_Succeeds()
    {
        int accountId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (_, type, institution) = SeedBaseGraph(dbContext);
            var account = new AccountEntity { Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = type };
            institution.Accounts.Add(account);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            accountId = account.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteAccountAsync(accountId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ReorderAccountsAsync_AppliesNewDisplayOrder()
    {
        var ids = new List<int>();
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (_, type, institution) = SeedBaseGraph(dbContext);
            var first = new AccountEntity { Name = "First", DisplayOrder = 1, UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = type };
            var second = new AccountEntity { Name = "Second", DisplayOrder = 2, UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = type };
            institution.Accounts.Add(first);
            institution.Accounts.Add(second);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            ids.Add(first.Id);
            ids.Add(second.Id);
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.ReorderAccountsAsync([ids[1], ids[0]]);

        Assert.True(result.IsSuccess);
        var accounts = await repository.GetAllAccountsAsync();
        Assert.Equal(["Second", "First"], accounts.Select(a => a.Name));
    }
}
