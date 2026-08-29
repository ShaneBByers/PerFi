using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class AccountTypeRepositoryTests
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

    private static AccountTypeGroupEntity SeedGroup(PerFiDbContext dbContext, int id = 1)
    {
        var group = new AccountTypeGroupEntity { Id = id, Name = "Assets", UserId = FakeCurrentUserService.DefaultUserId, AccountTypes = [] };
        dbContext.AccountTypeGroups.Add(group);
        return group;
    }

    [Fact]
    public async Task AddAccountTypeAsync_WithMissingGroup_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddAccountTypeAsync(new PerFi.Domain.Entities.AccountType("Checking", new PerFi.Domain.Entities.AccountTypeGroup("Assets")), 99);

        Assert.True(result.IsFailure);
        Assert.Contains("Account type group with ID", result.Error);
    }

    [Fact]
    public async Task AddAccountTypeAsync_WithValidGroup_ReturnsSuccessAndAssignsDisplayOrder()
    {
        var options = await CreateSeededOptionsAsync(dbContext => { SeedGroup(dbContext); return Task.CompletedTask; });
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddAccountTypeAsync(new PerFi.Domain.Entities.AccountType("Checking", new PerFi.Domain.Entities.AccountTypeGroup(1, "Assets")), 1);

        Assert.True(result.IsSuccess);
        var created = await repository.GetAccountTypeByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal(1, created!.DisplayOrder);
    }

    [Fact]
    public async Task GetAccountTypeByIdAsync_ForOtherUser_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            var group = new AccountTypeGroupEntity { Id = 1, Name = "Assets", UserId = "other-user", AccountTypes = [] };
            dbContext.AccountTypeGroups.Add(group);
            dbContext.AccountTypes.Add(new AccountTypeEntity { Id = 1, Name = "Checking", UserId = "other-user", AccountTypeGroupId = 1, AccountTypeGroup = group, Accounts = [] });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetAccountTypeByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAccountTypeAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext => { SeedGroup(dbContext); return Task.CompletedTask; });
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateAccountTypeAsync(new PerFi.Domain.Entities.AccountType(99, "Checking", new PerFi.Domain.Entities.AccountTypeGroup(1, "Assets")), 1);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateAccountTypeAsync_WithMissingGroup_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var group = SeedGroup(dbContext);
            dbContext.AccountTypes.Add(new AccountTypeEntity { Id = 1, Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, AccountTypeGroupId = 1, AccountTypeGroup = group, Accounts = [] });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateAccountTypeAsync(new PerFi.Domain.Entities.AccountType(1, "Savings", new PerFi.Domain.Entities.AccountTypeGroup(99, "Assets")), 99);

        Assert.True(result.IsFailure);
        Assert.Contains("Account type group with ID", result.Error);
    }

    [Fact]
    public async Task UpdateAccountTypeAsync_WithValidData_UpdatesName()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var group = SeedGroup(dbContext);
            dbContext.AccountTypes.Add(new AccountTypeEntity { Id = 1, Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, AccountTypeGroupId = 1, AccountTypeGroup = group, Accounts = [] });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateAccountTypeAsync(new PerFi.Domain.Entities.AccountType(1, "Savings", new PerFi.Domain.Entities.AccountTypeGroup(1, "Assets")), 1);

        Assert.True(result.IsSuccess);
        var updated = await repository.GetAccountTypeByIdAsync(1);
        Assert.Equal("Savings", updated!.Name);
    }

    [Fact]
    public async Task DeleteAccountTypeAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteAccountTypeAsync(99);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReorderAccountTypesAsync_AppliesNewDisplayOrder()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var group = SeedGroup(dbContext);
            dbContext.AccountTypes.Add(new AccountTypeEntity { Id = 1, Name = "Checking", DisplayOrder = 1, UserId = FakeCurrentUserService.DefaultUserId, AccountTypeGroupId = 1, AccountTypeGroup = group, Accounts = [] });
            dbContext.AccountTypes.Add(new AccountTypeEntity { Id = 2, Name = "Savings", DisplayOrder = 2, UserId = FakeCurrentUserService.DefaultUserId, AccountTypeGroupId = 1, AccountTypeGroup = group, Accounts = [] });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountTypeRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.ReorderAccountTypesAsync([2, 1]);

        Assert.True(result.IsSuccess);
        var types = await repository.GetAllAccountTypesAsync();
        Assert.Equal(["Savings", "Checking"], types.Select(t => t.Name));
    }
}
