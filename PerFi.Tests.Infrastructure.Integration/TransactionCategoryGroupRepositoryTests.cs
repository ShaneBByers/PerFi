using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class TransactionCategoryGroupRepositoryTests
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
    public async Task AddTransactionCategoryGroupAsync_ThenGetById_ReturnsCreatedGroup()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddTransactionCategoryGroupAsync(new TransactionCategoryGroup("Expenses"));

        Assert.True(result.IsSuccess);
        var created = await repository.GetTransactionCategoryGroupByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal("Expenses", created!.Name);
    }

    [Fact]
    public async Task GetTransactionCategoryGroupByIdAsync_ForOtherUser_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            dbContext.TransactionCategoryGroups.Add(new TransactionCategoryGroupEntity
            {
                Id = 1,
                Name = "Other",
                UserId = "other-user",
                DisplayOrder = 1,
                TransactionCategories = []
            });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetTransactionCategoryGroupByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTransactionCategoryGroupAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateTransactionCategoryGroupAsync(new TransactionCategoryGroup(99, "Expenses"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateTransactionCategoryGroupAsync_WithExistingGroup_UpdatesName()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.TransactionCategoryGroups.Add(new TransactionCategoryGroupEntity
            {
                Id = 1,
                Name = "Expenses",
                UserId = FakeCurrentUserService.DefaultUserId,
                DisplayOrder = 1,
                TransactionCategories = []
            });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateTransactionCategoryGroupAsync(new TransactionCategoryGroup(1, "Income"));

        Assert.True(result.IsSuccess);
        var updated = await repository.GetTransactionCategoryGroupByIdAsync(1);
        Assert.Equal("Income", updated!.Name);
    }

    [Fact]
    public async Task DeleteTransactionCategoryGroupAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteTransactionCategoryGroupAsync(99);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteTransactionCategoryGroupAsync_WhenReferencedByCategory_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var group = new TransactionCategoryGroupEntity
            {
                Id = 1,
                Name = "Expenses",
                UserId = FakeCurrentUserService.DefaultUserId,
                DisplayOrder = 1,
                TransactionCategories = []
            };
            dbContext.TransactionCategoryGroups.Add(group);
            dbContext.TransactionCategories.Add(new TransactionCategoryEntity
            {
                Id = 1,
                Name = "Groceries",
                UserId = FakeCurrentUserService.DefaultUserId,
                DisplayOrder = 1,
                TransactionCategoryGroupId = 1,
                TransactionCategoryGroup = group,
                Transactions = []
            });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteTransactionCategoryGroupAsync(1);

        Assert.True(result.IsFailure);
        Assert.Contains("reference", result.Error);
    }

    [Fact]
    public async Task ReorderTransactionCategoryGroupsAsync_AppliesNewDisplayOrder()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.TransactionCategoryGroups.Add(new TransactionCategoryGroupEntity
            {
                Id = 1,
                Name = "Expenses",
                DisplayOrder = 1,
                UserId = FakeCurrentUserService.DefaultUserId,
                TransactionCategories = []
            });
            dbContext.TransactionCategoryGroups.Add(new TransactionCategoryGroupEntity
            {
                Id = 2,
                Name = "Income",
                DisplayOrder = 2,
                UserId = FakeCurrentUserService.DefaultUserId,
                TransactionCategories = []
            });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryGroupRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.ReorderTransactionCategoryGroupsAsync([2, 1]);

        Assert.True(result.IsSuccess);
        var groups = await repository.GetAllTransactionCategoryGroupsAsync();
        Assert.Equal(["Income", "Expenses"], groups.Select(g => g.Name));
    }
}
