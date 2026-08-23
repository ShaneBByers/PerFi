using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using Xunit;

namespace PerFi.Tests.Integration;

public sealed class InstitutionRepositoryTests
{
    [Fact]
    public async Task GetAllInstitutionsAsync_ReturnsDisplayOrderSequence()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PerFiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new PerFiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            await SeedInstitutionsAsync(setupContext);
        }

        await using var verifyContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(verifyContext, new FakeCurrentUserService());

        var institutions = await repository.GetAllInstitutionsAsync();

        Assert.Equal(["First Bank", "Second Bank"], institutions.Select(institution => institution.Name));
        Assert.Equal([1, 2], institutions.Select(institution => institution.DisplayOrder));
        Assert.Equal(["Alpha", "Zeta"], institutions.First(institution => institution.Name == "First Bank").Accounts.Select(account => account.Name));
        Assert.Equal([1, 2], institutions.First(institution => institution.Name == "First Bank").Accounts.Select(account => account.DisplayOrder));
    }

    private static async Task SeedInstitutionsAsync(PerFiDbContext dbContext)
    {
        dbContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });

        var group = new AccountTypeGroupEntity
        {
            Id = 1,
            Name = "Assets",
            DisplayOrder = 1,
            UserId = FakeCurrentUserService.DefaultUserId,
            AccountTypes = []
        };

        var accountType = new AccountTypeEntity
        {
            Id = 1,
            Name = "Checking",
            DisplayOrder = 1,
            AccountTypeGroupId = 1,
            AccountTypeGroup = group,
            Accounts = []
        };

        group.AccountTypes.Add(accountType);

        var firstInstitution = new InstitutionEntity
        {
            Id = 2,
            Name = "First Bank",
            DisplayOrder = 1,
            UserId = FakeCurrentUserService.DefaultUserId,
            Accounts = []
        };

        var secondInstitution = new InstitutionEntity
        {
            Id = 1,
            Name = "Second Bank",
            DisplayOrder = 2,
            UserId = FakeCurrentUserService.DefaultUserId,
            Accounts = []
        };

        var firstAccount = new AccountEntity
        {
            Id = 1,
            Name = "Alpha",
            DisplayOrder = 1,
            InstitutionId = firstInstitution.Id,
            Institution = firstInstitution,
            AccountTypeId = accountType.Id,
            AccountType = accountType
        };

        var secondAccount = new AccountEntity
        {
            Id = 2,
            Name = "Zeta",
            DisplayOrder = 2,
            InstitutionId = firstInstitution.Id,
            Institution = firstInstitution,
            AccountTypeId = accountType.Id,
            AccountType = accountType
        };

        var thirdAccount = new AccountEntity
        {
            Id = 3,
            Name = "Beta",
            DisplayOrder = 1,
            InstitutionId = secondInstitution.Id,
            Institution = secondInstitution,
            AccountTypeId = accountType.Id,
            AccountType = accountType
        };

        firstInstitution.Accounts.Add(firstAccount);
        firstInstitution.Accounts.Add(secondAccount);
        secondInstitution.Accounts.Add(thirdAccount);
        accountType.Accounts.Add(firstAccount);
        accountType.Accounts.Add(secondAccount);
        accountType.Accounts.Add(thirdAccount);

        dbContext.AccountTypeGroups.Add(group);
        dbContext.AccountTypes.Add(accountType);
        dbContext.Institutions.Add(firstInstitution);
        dbContext.Institutions.Add(secondInstitution);
        dbContext.Accounts.Add(firstAccount);
        dbContext.Accounts.Add(secondAccount);
        dbContext.Accounts.Add(thirdAccount);

        await dbContext.SaveChangesAsync();
    }
}