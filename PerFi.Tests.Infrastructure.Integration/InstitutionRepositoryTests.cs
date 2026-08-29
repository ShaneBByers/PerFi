using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Infrastructure.Integration;

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
            UserId = FakeCurrentUserService.DefaultUserId,
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
            UserId = FakeCurrentUserService.DefaultUserId,
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
            UserId = FakeCurrentUserService.DefaultUserId,
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
            UserId = FakeCurrentUserService.DefaultUserId,
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

    private static async Task<DbContextOptions<PerFiDbContext>> CreateEmptySeededOptionsAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using var setupContext = new PerFiDbContext(options);
        await setupContext.Database.EnsureCreatedAsync();
        setupContext.Users.Add(new ApplicationUser { Id = FakeCurrentUserService.DefaultUserId, UserName = "test-user" });
        await setupContext.SaveChangesAsync();

        return options;
    }

    [Fact]
    public async Task GetInstitutionByIdAsync_ForOtherUser_ReturnsNull()
    {
        var options = await CreateEmptySeededOptionsAsync();
        await using (var setupContext = new PerFiDbContext(options))
        {
            setupContext.Users.Add(new ApplicationUser { Id = "other-user", UserName = "other" });
            setupContext.Institutions.Add(new InstitutionEntity { Id = 1, Name = "Theirs", UserId = "other-user", Accounts = [] });
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.GetInstitutionByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddInstitutionAsync_WithDuplicateName_ReturnsFailure()
    {
        var options = await CreateEmptySeededOptionsAsync();
        await using (var setupContext = new PerFiDbContext(options))
        {
            setupContext.Institutions.Add(new InstitutionEntity { Name = "First Bank", UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] });
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddInstitutionAsync(new PerFi.Domain.Entities.Institution("First Bank", []));

        Assert.True(result.IsFailure);
    }

    [Fact]    public async Task AddInstitutionAsync_WithNewName_ReturnsSuccess()
    {
        var options = await CreateEmptySeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.AddInstitutionAsync(new PerFi.Domain.Entities.Institution("Second Bank", []));

        Assert.True(result.IsSuccess);
        var created = await repository.GetInstitutionByIdAsync(result.Value);
        Assert.NotNull(created);
        Assert.Equal("Second Bank", created!.Name);
        Assert.Empty(created.Accounts);
    }

    [Fact]
    public async Task UpdateInstitutionAsync_WithDuplicateName_ReturnsFailure()
    {
        int institutionId = 0;
        var options = await CreateEmptySeededOptionsAsync();
        await using (var setupContext = new PerFiDbContext(options))
        {
            setupContext.Institutions.Add(new InstitutionEntity { Name = "First Bank", UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] });
            var second = new InstitutionEntity { Name = "Second Bank", UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] };
            setupContext.Institutions.Add(second);
            await setupContext.SaveChangesAsync();
            institutionId = second.Id;
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateInstitutionAsync(new PerFi.Domain.Entities.Institution(institutionId, "First Bank", []));

        Assert.True(result.IsFailure);
    }

    [Fact]    public async Task UpdateInstitutionAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateEmptySeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateInstitutionAsync(new PerFi.Domain.Entities.Institution(99, "Second Bank", []));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateInstitutionAsync_WithValidData_UpdatesName()
    {
        int institutionId = 0;
        var options = await CreateEmptySeededOptionsAsync();
        await using (var setupContext = new PerFiDbContext(options))
        {
            var institution = new InstitutionEntity { Name = "First Bank", UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] };
            setupContext.Institutions.Add(institution);
            await setupContext.SaveChangesAsync();
            institutionId = institution.Id;
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.UpdateInstitutionAsync(new PerFi.Domain.Entities.Institution(institutionId, "Renamed Bank", []));

        Assert.True(result.IsSuccess);
        var updated = await repository.GetInstitutionByIdAsync(institutionId);
        Assert.Equal("Renamed Bank", updated!.Name);
    }

    [Fact]
    public async Task DeleteInstitutionAsync_WhenMissing_ReturnsFailure()
    {
        var options = await CreateEmptySeededOptionsAsync();
        await using var dbContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteInstitutionAsync(99);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteInstitutionAsync_WhenReferencedByAccount_ReturnsFailure()
    {
        int institutionId = 0;
        var options = await CreateEmptySeededOptionsAsync();
        await using (var setupContext = new PerFiDbContext(options))
        {
            var group = new AccountTypeGroupEntity { Name = "Assets", UserId = FakeCurrentUserService.DefaultUserId, AccountTypes = [] };
            var type = new AccountTypeEntity { Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, AccountTypeGroup = group, Accounts = [] };
            group.AccountTypes.Add(type);
            var institution = new InstitutionEntity { Name = "First Bank", UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] };
            institution.Accounts.Add(new AccountEntity { Name = "Checking", UserId = FakeCurrentUserService.DefaultUserId, Institution = institution, AccountType = type });

            setupContext.AccountTypeGroups.Add(group);
            setupContext.AccountTypes.Add(type);
            setupContext.Institutions.Add(institution);
            await setupContext.SaveChangesAsync();
            institutionId = institution.Id;
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.DeleteInstitutionAsync(institutionId);

        Assert.True(result.IsFailure);
        Assert.Contains("reference", result.Error);
    }

    [Fact]
    public async Task ReorderInstitutionsAsync_AppliesNewDisplayOrder()
    {
        var ids = new List<int>();
        var options = await CreateEmptySeededOptionsAsync();
        await using (var setupContext = new PerFiDbContext(options))
        {
            var first = new InstitutionEntity { Name = "First Bank", DisplayOrder = 1, UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] };
            var second = new InstitutionEntity { Name = "Second Bank", DisplayOrder = 2, UserId = FakeCurrentUserService.DefaultUserId, Accounts = [] };
            setupContext.Institutions.Add(first);
            setupContext.Institutions.Add(second);
            await setupContext.SaveChangesAsync();
            ids.Add(first.Id);
            ids.Add(second.Id);
        }

        await using var dbContext = new PerFiDbContext(options);
        var repository = new InstitutionRepository(dbContext, new FakeCurrentUserService());

        var result = await repository.ReorderInstitutionsAsync([ids[1], ids[0]]);

        Assert.True(result.IsSuccess);
        var institutions = await repository.GetAllInstitutionsAsync();
        Assert.Equal(["Second Bank", "First Bank"], institutions.Select(i => i.Name));
    }
}
