using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class TransactionCategoryRepositoryTests
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

    private static TransactionCategoryGroupEntity SeedGroup(PerFiDbContext dbContext, int id = 1)
    {
        var group = new TransactionCategoryGroupEntity
        {
            Id = id,
            Name = "Expenses",
            UserId = FakeCurrentUserService.DefaultUserId,
            DisplayOrder = 1,
            TransactionCategories = []
        };
        dbContext.TransactionCategoryGroups.Add(group);
        return group;
    }

    [Fact]
    public async Task AddTransactionCategoryAsync_WithMissingGroup_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddTransactionCategoryAsync(new TransactionCategory(
            "Groceries",
            new TransactionCategoryGroup(99, "Unknown")), 99);

        Assert.True(result.IsFailure);
        Assert.Contains("Transaction category group with ID", result.Error);
    }

    [Fact]
    public async Task AddTransactionCategoryAsync_WithValidGroup_ReturnsSuccessAndAssignsDisplayOrder()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            SeedGroup(dbContext);
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddTransactionCategoryAsync(new TransactionCategory(
            "Groceries",
            new TransactionCategoryGroup(1, "Expenses")), 1);

        Assert.True(result.IsSuccess);
        var created = await repository.GetTransactionCategoryByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal(1, created!.DisplayOrder);
    }

    [Fact]
    public async Task GetTransactionCategoryByIdAsync_ForOtherUser_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            var group = new TransactionCategoryGroupEntity
            {
                Id = 1,
                Name = "Other Group",
                UserId = "other-user",
                DisplayOrder = 1,
                TransactionCategories = []
            };
            dbContext.TransactionCategoryGroups.Add(group);
            dbContext.TransactionCategories.Add(new TransactionCategoryEntity
            {
                Id = 1,
                Name = "Other Category",
                UserId = "other-user",
                DisplayOrder = 1,
                TransactionCategoryGroupId = 1,
                TransactionCategoryGroup = group,
                Transactions = []
            });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetTransactionCategoryByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTransactionCategoryAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            SeedGroup(dbContext);
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateTransactionCategoryAsync(new TransactionCategory(
            99,
            "Groceries",
            new TransactionCategoryGroup(1, "Expenses")), 1);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateTransactionCategoryAsync_WithMissingGroup_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var group = SeedGroup(dbContext);
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
        var repository = new TransactionCategoryRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateTransactionCategoryAsync(new TransactionCategory(
            1,
            "Rent",
            new TransactionCategoryGroup(99, "Missing")), 99);

        Assert.True(result.IsFailure);
        Assert.Contains("Transaction category group with ID", result.Error);
    }

    [Fact]
    public async Task UpdateTransactionCategoryAsync_WithValidData_UpdatesName()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var group = SeedGroup(dbContext);
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
        var repository = new TransactionCategoryRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateTransactionCategoryAsync(new TransactionCategory(
            1,
            "Rent",
            new TransactionCategoryGroup(1, "Expenses")), 1);

        Assert.True(result.IsSuccess);
        var updated = await repository.GetTransactionCategoryByIdAsync(1);
        Assert.Equal("Rent", updated!.Name);
    }

    [Fact]
    public async Task DeleteTransactionCategoryAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteTransactionCategoryAsync(99);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReorderTransactionCategoriesAsync_AppliesNewDisplayOrder()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var group = SeedGroup(dbContext);
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
            dbContext.TransactionCategories.Add(new TransactionCategoryEntity
            {
                Id = 2,
                Name = "Rent",
                UserId = FakeCurrentUserService.DefaultUserId,
                DisplayOrder = 2,
                TransactionCategoryGroupId = 1,
                TransactionCategoryGroup = group,
                Transactions = []
            });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionCategoryRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.ReorderTransactionCategoriesAsync([2, 1]);

        Assert.True(result.IsSuccess);
        var categories = await repository.GetAllTransactionCategoriesAsync();
        Assert.Equal(["Rent", "Groceries"], categories.Select(c => c.Name));
    }
}