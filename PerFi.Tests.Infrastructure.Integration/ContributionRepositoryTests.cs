using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

public sealed class ContributionRepositoryTests
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

    private static (AccountEntity Account, ContributionContributorEntity Contributor) SeedBaseGraph(
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

        var contributor = new ContributionContributorEntity
        {
            Name = $"{namePrefix}Contributor",
            UserId = userId,
            DisplayOrder = 1,
            Contributions = []
        };

        dbContext.AccountTypeGroups.Add(accountTypeGroup);
        dbContext.AccountTypes.Add(accountType);
        dbContext.Institutions.Add(institution);
        dbContext.Accounts.Add(account);
        dbContext.ContributionContributors.Add(contributor);

        return (account, contributor);
    }

    private static ContributionContributor CreateDomainContributor(int id, string name = "Contributor")
        => new(id, name) { DisplayOrder = 1 };

    [Fact]
    public async Task GetAllContributionsAsync_OnlyReturnsCurrentUsersContributions()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (myAccount, myContributor) = SeedBaseGraph(dbContext);
            dbContext.Contributions.Add(new ContributionEntity
            {
                Date = new DateOnly(2026, 1, 2),
                Amount = 10m,
                UserId = FakeCurrentUserService.DefaultUserId,
                Contributor = myContributor,
                Account = myAccount
            });

            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            var (otherAccount, otherContributor) = SeedBaseGraph(dbContext, "other-user", "Other ");
            dbContext.Contributions.Add(new ContributionEntity
            {
                Date = new DateOnly(2026, 1, 3),
                Amount = 99m,
                UserId = "other-user",
                Contributor = otherContributor,
                Account = otherAccount
            });

            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionRepository(dbContext, new FakeCurrentUserService());

        var contributions = await repository.GetAllContributionsAsync();

        var contribution = Assert.Single(contributions);
        Assert.Equal(10m, contribution.Amount);
    }

    [Fact]
    public async Task GetContributionByIdAsync_ForOtherUser_ReturnsNull()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            dbContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            var (otherAccount, otherContributor) = SeedBaseGraph(dbContext, "other-user", "Other ");
            dbContext.Contributions.Add(new ContributionEntity
            {
                Id = 1,
                Date = new DateOnly(2026, 1, 1),
                Amount = 5m,
                UserId = "other-user",
                Contributor = otherContributor,
                Account = otherAccount
            });

            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionRepository(dbContext, new FakeCurrentUserService());

        var contribution = await repository.GetContributionByIdAsync(1);

        Assert.Null(contribution);
    }

    [Fact]
    public async Task AddContributionAsync_WithMissingContributor_ReturnsFailure()
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
        var repository = new ContributionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddContributionAsync(new Contribution(
            new DateOnly(2026, 2, 1),
            12.34m,
            CreateDomainContributor(999),
            accountId));

        Assert.True(result.IsFailure);
        Assert.Contains("Contribution contributor", result.Error);
    }

    [Fact]
    public async Task AddContributionAsync_WithMissingAccount_ReturnsFailure()
    {
        int contributorId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (_, contributor) = SeedBaseGraph(dbContext);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            contributorId = contributor.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddContributionAsync(new Contribution(
            new DateOnly(2026, 2, 1),
            12.34m,
            CreateDomainContributor(contributorId),
            999));

        Assert.True(result.IsFailure);
        Assert.Contains("Account", result.Error);
    }

    [Fact]
    public async Task AddContributionAsync_WithValidData_ReturnsSuccessAndPersistsRecord()
    {
        int accountId = 0;
        int contributorId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (account, contributor) = SeedBaseGraph(dbContext);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            accountId = account.Id;
            contributorId = contributor.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddContributionAsync(new Contribution(
            new DateOnly(2026, 2, 1),
            7.5m,
            CreateDomainContributor(contributorId),
            accountId));

        Assert.True(result.IsSuccess);
        var created = await repository.GetContributionByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal(7.5m, created!.Amount);
    }

    [Fact]
    public async Task UpdateContributionAsync_WhenMissing_ReturnsFailure()
    {
        int accountId = 0;
        int contributorId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (account, contributor) = SeedBaseGraph(dbContext);
            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            accountId = account.Id;
            contributorId = contributor.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateContributionAsync(new Contribution(
            id: 999,
            date: new DateOnly(2026, 2, 1),
            amount: 1m,
            contributor: CreateDomainContributor(contributorId),
            accountId: accountId));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateContributionAsync_WithValidData_UpdatesPersistedValues()
    {
        int contributionId = 0;
        int updatedAccountId = 0;
        int updatedContributorId = 0;

        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (account, contributor) = SeedBaseGraph(dbContext);
            var (secondAccount, secondContributor) = SeedBaseGraph(dbContext, namePrefix: "Second ");

            dbContext.Contributions.Add(new ContributionEntity
            {
                Date = new DateOnly(2026, 1, 1),
                Amount = 2m,
                UserId = FakeCurrentUserService.DefaultUserId,
                Contributor = contributor,
                Account = account
            });

            dbContext.SaveChangesAsync().GetAwaiter().GetResult();

            contributionId = dbContext.Contributions.Single(c => c.Amount == 2m).Id;
            updatedAccountId = secondAccount.Id;
            updatedContributorId = secondContributor.Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateContributionAsync(new Contribution(
            contributionId,
            new DateOnly(2026, 3, 3),
            88m,
            CreateDomainContributor(updatedContributorId),
            updatedAccountId));

        Assert.True(result.IsSuccess);
        var updated = await repository.GetContributionByIdAsync(contributionId);
        Assert.Equal(88m, updated!.Amount);
        Assert.Equal(updatedAccountId, updated.AccountId);
        Assert.Equal(updatedContributorId, updated.Contributor.Id);
    }

    [Fact]
    public async Task DeleteContributionAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            SeedBaseGraph(dbContext);
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteContributionAsync(999);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteContributionAsync_WithExistingContribution_Succeeds()
    {
        int contributionId = 0;
        var options = await CreateSeededOptionsAsync(dbContext =>
        {
            var (account, contributor) = SeedBaseGraph(dbContext);

            dbContext.Contributions.Add(new ContributionEntity
            {
                Date = new DateOnly(2026, 1, 1),
                Amount = 11m,
                UserId = FakeCurrentUserService.DefaultUserId,
                Contributor = contributor,
                Account = account
            });

            dbContext.SaveChangesAsync().GetAwaiter().GetResult();
            contributionId = dbContext.Contributions.Single(c => c.Amount == 11m).Id;
            return Task.CompletedTask;
        });

        await using var dbContext = new PerFiDbContext(options);
        var repository = new ContributionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteContributionAsync(contributionId);

        Assert.True(result.IsSuccess);
        var deleted = await repository.GetContributionByIdAsync(contributionId);
        Assert.Null(deleted);
    }
}
