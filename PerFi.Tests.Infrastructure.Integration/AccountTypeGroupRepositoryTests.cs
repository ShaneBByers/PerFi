using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class AccountTypeGroupRepositoryTests
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

    [Fact]
    public async Task AddAccountTypeGroupAsync_ThenGetById_ReturnsCreatedGroup()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddAccountTypeGroupAsync(new PerFi.Domain.Entities.AccountTypeGroup("Assets"));

        Assert.True(result.IsSuccess);
        var created = await repository.GetAccountTypeGroupByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal("Assets", created!.Name);
    }

    [Fact]
    public async Task GetAccountTypeGroupByIdAsync_ForOtherUser_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            dbContext.AccountTypeGroups.Add(new AccountTypeGroupEntity { Id = 1, Name = "Assets", UserId = "other-user", AccountTypes = [] });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetAccountTypeGroupByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAccountTypeGroupAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateAccountTypeGroupAsync(new PerFi.Domain.Entities.AccountTypeGroup(99, "Assets"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateAccountTypeGroupAsync_WithExistingGroup_UpdatesName()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.AccountTypeGroups.Add(new AccountTypeGroupEntity { Id = 1, Name = "Assets", UserId = FakeCurrentUserService.DefaultUserId, AccountTypes = [] });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateAccountTypeGroupAsync(new PerFi.Domain.Entities.AccountTypeGroup(1, "Liabilities"));

        Assert.True(result.IsSuccess);
        var updated = await repository.GetAccountTypeGroupByIdAsync(1);
        Assert.Equal("Liabilities", updated!.Name);
    }

    [Fact]
    public async Task DeleteAccountTypeGroupAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteAccountTypeGroupAsync(99);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteAccountTypeGroupAsync_WhenReferencedByAccountType_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var group = new AccountTypeGroupEntity { Id = 1, Name = "Assets", UserId = FakeCurrentUserService.DefaultUserId, AccountTypes = [] };
            dbContext.AccountTypeGroups.Add(group);
            dbContext.AccountTypes.Add(new AccountTypeEntity { Id = 1, Name = "Checking", AccountTypeGroupId = 1, AccountTypeGroup = group, Accounts = [] });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteAccountTypeGroupAsync(1);

        Assert.True(result.IsFailure);
        Assert.Contains("reference", result.Error);
    }

    [Fact]
    public async Task ReorderAccountTypeGroupsAsync_AppliesNewDisplayOrder()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.AccountTypeGroups.Add(new AccountTypeGroupEntity { Id = 1, Name = "Assets", DisplayOrder = 1, UserId = FakeCurrentUserService.DefaultUserId, AccountTypes = [] });
            dbContext.AccountTypeGroups.Add(new AccountTypeGroupEntity { Id = 2, Name = "Liabilities", DisplayOrder = 2, UserId = FakeCurrentUserService.DefaultUserId, AccountTypes = [] });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.ReorderAccountTypeGroupsAsync([2, 1]);

        Assert.True(result.IsSuccess);
        var groups = await repository.GetAllAccountTypeGroupsAsync();
        Assert.Equal(["Liabilities", "Assets"], groups.Select(g => g.Name));
    }
}
