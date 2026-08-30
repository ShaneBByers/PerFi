using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class ContributionContributorRepositoryTests
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
    public async Task AddContributionContributorAsync_ThenGetById_ReturnsCreatedContributor()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionContributorRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddContributionContributorAsync(new ContributionContributor("Alex"));

        Assert.True(result.IsSuccess);
        var created = await repository.GetContributionContributorByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal("Alex", created!.Name);
        Assert.Equal(1, created.DisplayOrder);
    }

    [Fact]
    public async Task GetContributionContributorByIdAsync_ForOtherUser_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            dbContext.ContributionContributors.Add(new ContributionContributorEntity
            {
                Id = 1,
                Name = "Other",
                UserId = "other-user",
                DisplayOrder = 1,
                Contributions = []
            });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionContributorRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetContributionContributorByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateContributionContributorAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionContributorRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateContributionContributorAsync(new ContributionContributor(99, "Alex"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateContributionContributorAsync_WithExistingContributor_UpdatesName()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.ContributionContributors.Add(new ContributionContributorEntity
            {
                Id = 1,
                Name = "Alex",
                UserId = FakeCurrentUserService.DefaultUserId,
                DisplayOrder = 1,
                Contributions = []
            });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionContributorRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateContributionContributorAsync(new ContributionContributor(1, "Jordan"));

        Assert.True(result.IsSuccess);
        var updated = await repository.GetContributionContributorByIdAsync(1);
        Assert.Equal("Jordan", updated!.Name);
    }

    [Fact]
    public async Task DeleteContributionContributorAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(_ => Task.CompletedTask);
        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionContributorRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteContributionContributorAsync(99);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteContributionContributorAsync_WhenReferencedByContribution_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var accountTypeGroup = new AccountTypeGroupEntity { Name = "Assets", UserId = FakeCurrentUserService.DefaultUserId, AccountTypes = [] };
            var accountType = new AccountTypeEntity { Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, AccountTypeGroup = accountTypeGroup, Accounts = [] };
            accountTypeGroup.AccountTypes.Add(accountType);
            var institution = new InstitutionEntity { Name = "Bank", UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] };
            var account = new AccountEntity { Id = 1, Name = "Account", UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = accountType };
            institution.Accounts.Add(account);
            accountType.Accounts.Add(account);

            var contributor = new ContributionContributorEntity
            {
                Id = 1,
                Name = "Alex",
                UserId = FakeCurrentUserService.DefaultUserId,
                DisplayOrder = 1,
                Contributions = []
            };

            dbContext.AccountTypeGroups.Add(accountTypeGroup);
            dbContext.AccountTypes.Add(accountType);
            dbContext.Institutions.Add(institution);
            dbContext.Accounts.Add(account);
            dbContext.ContributionContributors.Add(contributor);
            dbContext.Contributions.Add(new ContributionEntity
            {
                Date = new DateOnly(2026, 1, 1),
                Amount = 10m,
                UserId = FakeCurrentUserService.DefaultUserId,
                ContributorId = 1,
                Contributor = contributor,
                AccountId = 1,
                Account = account
            });

            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionContributorRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteContributionContributorAsync(1);

        Assert.True(result.IsFailure);
        Assert.Contains("reference", result.Error);
    }

    [Fact]
    public async Task ReorderContributionContributorsAsync_AppliesNewDisplayOrder()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.ContributionContributors.Add(new ContributionContributorEntity
            {
                Id = 1,
                Name = "First",
                DisplayOrder = 1,
                UserId = FakeCurrentUserService.DefaultUserId,
                Contributions = []
            });
            dbContext.ContributionContributors.Add(new ContributionContributorEntity
            {
                Id = 2,
                Name = "Second",
                DisplayOrder = 2,
                UserId = FakeCurrentUserService.DefaultUserId,
                Contributions = []
            });
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionContributorRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.ReorderContributionContributorsAsync([2, 1]);

        Assert.True(result.IsSuccess);
        var contributors = await repository.GetAllContributionContributorsAsync();
        Assert.Equal(["Second", "First"], contributors.Select(c => c.Name));
    }
}
