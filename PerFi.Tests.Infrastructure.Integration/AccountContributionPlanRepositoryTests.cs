using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class AccountContributionPlanRepositoryTests
{
    private static async Task<(DbContextOptions<PerFiDbContext> Options, int AccountId)> CreateSeededOptionsAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using var setupContext = new PerFiDbContext(options);
        await setupContext.Database.EnsureCreatedAsync();
        setupContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });

        var accountTypeGroup = new AccountTypeGroupEntity { Name = "Assets", UserId = FakeCurrentUserService.DefaultUserId, AccountTypes = [] };
        var accountType = new AccountTypeEntity { Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, AccountTypeGroup = accountTypeGroup, Accounts = [] };
        accountTypeGroup.AccountTypes.Add(accountType);
        var institution = new InstitutionEntity { Name = "Bank", UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] };
        var account = new AccountEntity { Name = "Account", UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = accountType };
        institution.Accounts.Add(account);
        accountType.Accounts.Add(account);

        setupContext.AccountTypeGroups.Add(accountTypeGroup);
        setupContext.AccountTypes.Add(accountType);
        setupContext.Institutions.Add(institution);
        setupContext.Accounts.Add(account);
        await setupContext.SaveChangesAsync();

        return (options, account.Id);
    }

    [Fact]
    public async Task AddAsync_WithMissingAccount_ReturnsFailure()
    {
        var (options, _) = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountContributionPlanRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddAsync(new AccountContributionPlan(999, ContributionContributorType.Self, 100m, 10m, 0, 0, 0, 0, 0, 0));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddAsync_WithValidData_PersistsAndReturnsId()
    {
        var (options, accountId) = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountContributionPlanRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddAsync(new AccountContributionPlan(accountId, ContributionContributorType.Self, 100m, 10m, 0, 0, 0, 0, 0, 0));

        Assert.True(result.IsSuccess);
        var created = await repository.GetByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal(100m, created!.DollarAmountPerPayCycle);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateContributorTypeForAccount_ReturnsFailure()
    {
        var (options, accountId) = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountContributionPlanRepository(dbContext, new FakeCurrentUserService());

        var first = await repository.AddAsync(new AccountContributionPlan(accountId, ContributionContributorType.Self, 100m, 0, 0, 0, 0, 0, 0, 0));
        Assert.True(first.IsSuccess);

        var second = await repository.AddAsync(new AccountContributionPlan(accountId, ContributionContributorType.Self, 200m, 0, 0, 0, 0, 0, 0, 0));
        Assert.True(second.IsFailure);
        Assert.Contains("already exists", second.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllByAccountIdAsync_ReturnsOnlyPlansForThatAccount()
    {
        var (options, accountId) = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountContributionPlanRepository(dbContext, new FakeCurrentUserService());

        await repository.AddAsync(new AccountContributionPlan(accountId, ContributionContributorType.Self, 100m, 0, 0, 0, 0, 0, 0, 0));
        await repository.AddAsync(new AccountContributionPlan(accountId, ContributionContributorType.Employer, 50m, 0, 0, 0, 0, 0, 0, 0));

        var plans = await repository.GetAllByAccountIdAsync(accountId);

        Assert.Equal(2, plans.Count);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsFailure()
    {
        var (options, accountId) = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountContributionPlanRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateAsync(new AccountContributionPlan(999, accountId, ContributionContributorType.Self, 100m, 0, 0, 0, 0, 0, 0, 0));

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_RemovesPlan()
    {
        var (options, accountId) = await CreateSeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new AccountContributionPlanRepository(dbContext, new FakeCurrentUserService());

        var added = await repository.AddAsync(new AccountContributionPlan(accountId, ContributionContributorType.Self, 100m, 0, 0, 0, 0, 0, 0, 0));

        var result = await repository.DeleteAsync(added.Value);

        Assert.True(result.IsSuccess);
        Assert.Null(await repository.GetByIdAsync(added.Value));
    }
}
