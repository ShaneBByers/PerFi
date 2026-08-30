using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class TransactionRepositoryTests
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

    private static (AccountEntity Account, TransactionCategoryEntity Category) SeedBaseGraph(
        PerFiDbContext dbContext,
        string userId = FakeCurrentUserService.DefaultUserId,
        string namePrefix = "")
    {
        var accountTypeGroup = new AccountTypeGroupEntity
        {
            Name = $"{namePrefix}Assets",
            UserId = userId,
            AccountTypes = []
        };

        var accountType = new AccountTypeEntity
        {
            Name = $"{namePrefix}Checking",
            UserId = userId,
            AccountTypeGroup = accountTypeGroup,
            Accounts = []
        };
        accountTypeGroup.AccountTypes.Add(accountType);

        var institution = new InstitutionEntity
        {
            Name = $"{namePrefix}Bank",
            UserId = userId,
            Accounts = []
        };

        var account = new AccountEntity
        {
            Name = $"{namePrefix}Account",
            UserId = userId,
            Institution = institution,
            AccountType = accountType
        };
        institution.Accounts.Add(account);
        accountType.Accounts.Add(account);

        var categoryGroup = new TransactionCategoryGroupEntity
        {
            Name = $"{namePrefix}Expenses",
            UserId = userId,
            DisplayOrder = 1,
            TransactionCategories = []
        };

        var category = new TransactionCategoryEntity
        {
            Name = $"{namePrefix}Groceries",
            UserId = userId,
            DisplayOrder = 1,
            TransactionCategoryGroup = categoryGroup
        };
        categoryGroup.TransactionCategories.Add(category);

        dbContext.AccountTypeGroups.Add(accountTypeGroup);
        dbContext.AccountTypes.Add(accountType);
        dbContext.Institutions.Add(institution);
        dbContext.Accounts.Add(account);
        dbContext.TransactionCategoryGroups.Add(categoryGroup);
        dbContext.TransactionCategories.Add(category);

        return (account, category);
    }

    private static TransactionCategory CreateDomainCategory(int categoryId, string categoryName = "Groceries")
    {
        var group = new TransactionCategoryGroup(1, "Expenses") { DisplayOrder = 1 };
        return new TransactionCategory(categoryId, categoryName, group) { DisplayOrder = 1 };
    }

    [Fact]
    public async Task GetAllTransactionsAsync_OnlyReturnsCurrentUsersTransactions()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (myAccount, myCategory) = SeedBaseGraph(dbContext);
            dbContext.Transactions.Add(new TransactionEntity
            {
                Date = new DateOnly(2026, 1, 2),
                CounterpartyName = "Mine",
                Amount = 10m,
                UserId = FakeCurrentUserService.DefaultUserId,
                TransactionCategory = myCategory,
                Account = myAccount
            });

            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            var (otherAccount, otherCategory) = SeedBaseGraph(dbContext, "other-user", "Other ");
            dbContext.Transactions.Add(new TransactionEntity
            {
                Date = new DateOnly(2026, 1, 3),
                CounterpartyName = "Theirs",
                Amount = 99m,
                UserId = "other-user",
                TransactionCategory = otherCategory,
                Account = otherAccount
            });

            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionRepository(dbContext, new FakeCurrentUserService());

        var transactions = await repository.GetAllTransactionsAsync();

        var transaction = Assert.Single(transactions);
        Assert.Equal("Mine", transaction.CounterpartyName);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_ForOtherUser_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            var (otherAccount, otherCategory) = SeedBaseGraph(dbContext, "other-user", "Other ");
            dbContext.Transactions.Add(new TransactionEntity
            {
                Id = 1,
                Date = new DateOnly(2026, 1, 1),
                CounterpartyName = "Other Transaction",
                Amount = 5m,
                UserId = "other-user",
                TransactionCategory = otherCategory,
                Account = otherAccount
            });

            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionRepository(dbContext, new FakeCurrentUserService());

        var transaction = await repository.GetTransactionByIdAsync(1);

        Assert.Null(transaction);
    }

    [Fact]
    public async Task AddTransactionAsync_WithMissingTransactionCategory_ReturnsFailure()
    {
        int accountId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (account, _) = SeedBaseGraph(dbContext);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            accountId = account.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddTransactionAsync(new Transaction(
            new DateOnly(2026, 2, 1),
            "Store",
            12.34m,
            CreateDomainCategory(999),
            accountId));

        Assert.True(result.IsFailure);
        Assert.Contains("Transaction category", result.Error);
    }

    [Fact]
    public async Task AddTransactionAsync_WithMissingAccount_ReturnsFailure()
    {
        int categoryId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (_, category) = SeedBaseGraph(dbContext);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            categoryId = category.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddTransactionAsync(new Transaction(
            new DateOnly(2026, 2, 1),
            "Store",
            12.34m,
            CreateDomainCategory(categoryId),
            999));

        Assert.True(result.IsFailure);
        Assert.Contains("Account", result.Error);
    }

    [Fact]
    public async Task AddTransactionAsync_WithValidData_ReturnsSuccessAndPersistsRecord()
    {
        int accountId = 0;
        int categoryId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (account, category) = SeedBaseGraph(dbContext);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            accountId = account.Id;
            categoryId = category.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddTransactionAsync(new Transaction(
            new DateOnly(2026, 2, 1),
            "Coffee Shop",
            7.5m,
            CreateDomainCategory(categoryId),
            accountId,
            "Morning coffee"));

        Assert.True(result.IsSuccess);
        var created = await repository.GetTransactionByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal("Coffee Shop", created!.CounterpartyName);
        Assert.Equal(7.5m, created.Amount);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WhenMissing_ReturnsFailure()
    {
        int accountId = 0;
        int categoryId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (account, category) = SeedBaseGraph(dbContext);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            accountId = account.Id;
            categoryId = category.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateTransactionAsync(new Transaction(
            id: 999,
            date: new DateOnly(2026, 2, 1),
            counterpartyName: "Missing",
            amount: 1m,
            category: CreateDomainCategory(categoryId),
            accountId: accountId));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithValidData_UpdatesPersistedValues()
    {
        int transactionId = 0;
        int updatedAccountId = 0;
        int updatedCategoryId = 0;

        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (account, category) = SeedBaseGraph(dbContext);
            var (secondAccount, secondCategory) = SeedBaseGraph(dbContext, namePrefix: "Second ");

            dbContext.Transactions.Add(new TransactionEntity
            {
                Date = new DateOnly(2026, 1, 1),
                CounterpartyName = "Old",
                Amount = 2m,
                Description = "Old description",
                UserId = FakeCurrentUserService.DefaultUserId,
                TransactionCategory = category,
                Account = account
            });

            dbContext.SaveChangesAsync().GetAwaiter().GetResult();

            transactionId = dbContext.Transactions.Single(t => t.CounterpartyName == "Old").Id;
            updatedAccountId = secondAccount.Id;
            updatedCategoryId = secondCategory.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateTransactionAsync(new Transaction(
            transactionId,
            new DateOnly(2026, 3, 3),
            "New",
            88m,
            CreateDomainCategory(updatedCategoryId, "Second Groceries"),
            updatedAccountId,
            "New description"));

        Assert.True(result.IsSuccess);
        var updated = await repository.GetTransactionByIdAsync(transactionId);
        Assert.Equal("New", updated!.CounterpartyName);
        Assert.Equal(88m, updated.Amount);
        Assert.Equal(updatedAccountId, updated.AccountId);
        Assert.Equal(updatedCategoryId, updated.Category.Id);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            SeedBaseGraph(dbContext);
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteTransactionAsync(999);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WithExistingTransaction_Succeeds()
    {
        int transactionId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (account, category) = SeedBaseGraph(dbContext);

            dbContext.Transactions.Add(new TransactionEntity
            {
                Date = new DateOnly(2026, 1, 1),
                CounterpartyName = "Delete Me",
                Amount = 11m,
                UserId = FakeCurrentUserService.DefaultUserId,
                TransactionCategory = category,
                Account = account
            });

            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            transactionId = dbContext.Transactions.Single(t => t.CounterpartyName == "Delete Me").Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new TransactionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteTransactionAsync(transactionId);

        Assert.True(result.IsSuccess);
        var deleted = await repository.GetTransactionByIdAsync(transactionId);
        Assert.Null(deleted);
    }
}
