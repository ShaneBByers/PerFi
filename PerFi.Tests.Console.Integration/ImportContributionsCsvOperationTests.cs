using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Console;
using PerFi.Console.Import;
using PerFi.Console.Operations;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Console.Integration;

public sealed class ImportContributionsCsvOperationTests : IDisposable
{
    private readonly Microsoft.AspNetCore.Identity.UserManager<PerFi.Infrastructure.Entities.ApplicationUser> _userManager;
    private readonly PerFi.Infrastructure.PerFiDbContext _dbContext;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly ConsoleCurrentUserService _currentUser = new();

    public ImportContributionsCsvOperationTests()
    {
        (_userManager, _dbContext, _connection) = IdentityTestHelper.CreateUserManager();
    }

    private ImportContributionsCsvOperation CreateOperation(
        IInstitutionService? institutionService = null,
        IContributionContributorService? contributionContributorService = null,
        IContributionService? contributionService = null)
        => new(
            _dbContext,
            new ContributionCsvParser(),
            _userManager,
            _currentUser,
            institutionService ?? Mock.Of<IInstitutionService>(),
            contributionContributorService ?? Mock.Of<IContributionContributorService>(),
            contributionService ?? MockEmptyContributionService());

    private static Mock<IContributionService> MockContributionServiceMock()
    {
        var mock = new Mock<IContributionService>();
        mock.Setup(s => s.GetAllContributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Contribution>)[]);
        return mock;
    }

    private static IContributionService MockEmptyContributionService() => MockContributionServiceMock().Object;

    private static string CreateTempCsv(string csv)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"perfi-import-contributions-{Guid.NewGuid():N}.csv");
        File.WriteAllText(filePath, csv);
        return filePath;
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownUsername_ThrowsInvalidOperationException()
    {
        var operation = CreateOperation();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => operation.ExecuteAsync("does-not-matter.csv", "nobody", dryRun: true));
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingCsvFile_ThrowsFileNotFoundException()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var operation = CreateOperation();

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => operation.ExecuteAsync("/tmp/definitely-not-a-real-file-xyz.csv", "shane", dryRun: true));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCsv_ThrowsInvalidOperationException()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var filePath = CreateTempCsv(string.Empty);

        try
        {
            var operation = CreateOperation();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => operation.ExecuteAsync(filePath, "shane", dryRun: true));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUnresolvedAccount_ThrowsInvalidOperationException()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var filePath = CreateTempCsv("""
Date,Account Name,Institution,Contributor,Amount
2026-01-09,MiTek 401k,Fidelity,Me,$905.56
""");

        try
        {
            var institutionService = new Mock<IInstitutionService>();
            institutionService.Setup(s => s.GetAllInstitutionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Institution>)[]);

            var operation = CreateOperation(institutionService: institutionService.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => operation.ExecuteAsync(filePath, "shane", dryRun: true));

            Assert.Contains("unknown account", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingContributions_ThrowsInvalidOperationException()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var filePath = CreateTempCsv("""
Date,Account Name,Institution,Contributor,Amount
2026-01-09,MiTek 401k,Fidelity,Me,$905.56
""");

        try
        {
            var institutionService = new Mock<IInstitutionService>();
            institutionService.Setup(s => s.GetAllInstitutionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Institution>)[
                    new Institution(1, "Fidelity", [new Account(1, "MiTek 401k", new AccountType(1, "Retirement", new AccountTypeGroup(1, "Investments")))])
                ]);

            var contributionService = new Mock<IContributionService>();
            contributionService.Setup(s => s.GetAllContributionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Contribution>)[
                    new Contribution(1, new DateOnly(2026, 1, 1), 1m, new ContributionContributor(1, "Me"), 1)
                ]);

            var operation = CreateOperation(institutionService: institutionService.Object, contributionService: contributionService.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => operation.ExecuteAsync(filePath, "shane", dryRun: true));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_DryRun_DoesNotCreateAnyEntities()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var filePath = CreateTempCsv("""
Date,Account Name,Institution,Contributor,Amount
2026-01-09,MiTek 401k,Fidelity,Me,$905.56
""");

        try
        {
            var institutionService = new Mock<IInstitutionService>();
            institutionService.Setup(s => s.GetAllInstitutionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Institution>)[
                    new Institution(1, "Fidelity", [new Account(1, "MiTek 401k", new AccountType(1, "Retirement", new AccountTypeGroup(1, "Investments")))])
                ]);

            var contributorService = new Mock<IContributionContributorService>();
            var operation = CreateOperation(institutionService: institutionService.Object, contributionContributorService: contributorService.Object);

            await operation.ExecuteAsync(filePath, "shane", dryRun: true);

            contributorService.Verify(s => s.CreateContributionContributorAsync(It.IsAny<CreateContributionContributorCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NotDryRun_WithValidCsv_CreatesContributorAndContribution()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var filePath = CreateTempCsv("""
Date,Account Name,Institution,Contributor,Amount
2026-01-09,MiTek 401k,Fidelity,Me,$905.56
""");

        try
        {
            var institutionService = new Mock<IInstitutionService>();
            institutionService.Setup(s => s.GetAllInstitutionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Institution>)[
                    new Institution(1, "Fidelity", [new Account(1, "MiTek 401k", new AccountType(1, "Retirement", new AccountTypeGroup(1, "Investments")))])
                ]);

            var contributorService = new Mock<IContributionContributorService>();
            contributorService.Setup(s => s.GetAllContributionContributorsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<ContributionContributor>)[]);
            contributorService.Setup(s => s.CreateContributionContributorAsync(It.IsAny<CreateContributionContributorCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ContributionContributor>.Success(new ContributionContributor(1, "Me")));

            var contributionService = new Mock<IContributionService>();
            contributionService.Setup(s => s.GetAllContributionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Contribution>)[]);
            contributionService.Setup(s => s.CreateContributionAsync(It.IsAny<CreateContributionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Contribution>.Success(new Contribution(1, new DateOnly(2026, 1, 9), 905.56m, new ContributionContributor(1, "Me"), 1)));

            var operation = CreateOperation(
                institutionService.Object,
                contributorService.Object,
                contributionService.Object);

            await operation.ExecuteAsync(filePath, "shane", dryRun: false);

            contributorService.Verify(s => s.CreateContributionContributorAsync(It.IsAny<CreateContributionContributorCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            contributionService.Verify(s => s.CreateContributionAsync(It.IsAny<CreateContributionCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
