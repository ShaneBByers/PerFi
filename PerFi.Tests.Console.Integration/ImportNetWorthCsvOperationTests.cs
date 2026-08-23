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

public sealed class ImportNetWorthCsvOperationTests : IDisposable
{
    private readonly Microsoft.AspNetCore.Identity.UserManager<PerFi.Infrastructure.Entities.ApplicationUser> _userManager;
    private readonly PerFi.Infrastructure.PerFiDbContext _dbContext;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly ConsoleCurrentUserService _currentUser = new();

    public ImportNetWorthCsvOperationTests()
    {
        (_userManager, _dbContext, _connection) = IdentityTestHelper.CreateUserManager();
    }

    private ImportNetWorthCsvOperation CreateOperation(
        IInstitutionService? institutionService = null,
        IAccountTypeGroupService? accountTypeGroupService = null,
        IAccountTypeService? accountTypeService = null,
        IAccountService? accountService = null,
        IFinanceSnapshotService? financeSnapshotService = null)
        => new(
            _dbContext,
            new NetWorthCsvParser(),
            _userManager,
            _currentUser,
            institutionService ?? Mock.Of<IInstitutionService>(),
            accountTypeGroupService ?? Mock.Of<IAccountTypeGroupService>(),
            accountTypeService ?? Mock.Of<IAccountTypeService>(),
            accountService ?? Mock.Of<IAccountService>(),
            financeSnapshotService ?? Mock.Of<IFinanceSnapshotService>());

    private static string CreateTempCsv(string csv)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"perfi-import-{Guid.NewGuid():N}.csv");
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
    public async Task ExecuteAsync_DryRun_DoesNotCreateAnyEntities()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var filePath = CreateTempCsv("""
Institution,Account Type Group,Account Type,Account Name,1/1/2026
Bank A,Investments,IRA,Primary,$100.00
""");

        try
        {
            var institutionService = new Mock<IInstitutionService>();
            var operation = CreateOperation(institutionService: institutionService.Object);

            await operation.ExecuteAsync(filePath, "shane", dryRun: true);

            institutionService.Verify(s => s.CreateInstitutionAsync(It.IsAny<CreateInstitutionCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NotDryRun_WithValidCsv_CreatesEntitiesAndSnapshot()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var filePath = CreateTempCsv("""
Institution,Account Type Group,Account Type,Account Name,1/1/2026
Bank A,Investments,IRA,Primary,$100.00
""");

        try
        {
            var groupService = new Mock<IAccountTypeGroupService>();
            groupService.Setup(s => s.GetAllAccountTypeGroupsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<AccountTypeGroup>)[]);
            groupService.Setup(s => s.CreateAccountTypeGroupAsync(It.IsAny<CreateAccountTypeGroupCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<AccountTypeGroup>.Success(new AccountTypeGroup(1, "Investments")));

            var typeService = new Mock<IAccountTypeService>();
            typeService.Setup(s => s.GetAllAccountTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<AccountType>)[]);
            typeService.Setup(s => s.CreateAccountTypeAsync(It.IsAny<CreateAccountTypeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<AccountType>.Success(new AccountType(1, "IRA", new AccountTypeGroup(1, "Investments"))));

            var institutionService = new Mock<IInstitutionService>();
            institutionService.Setup(s => s.CreateInstitutionAsync(It.IsAny<CreateInstitutionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Institution>.Success(new Institution(1, "Bank A", [])));

            var accountService = new Mock<IAccountService>();
            accountService.Setup(s => s.CreateAccountAsync(It.IsAny<CreateAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Account>.Success(new Account(1, "Primary", new AccountType(1, "IRA", new AccountTypeGroup(1, "Investments")))));

            var snapshotService = new Mock<IFinanceSnapshotService>();
            snapshotService.Setup(s => s.CreateSnapshotAsync(It.IsAny<CreateFinanceSnapshotCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<FinanceSnapshot>.Success(new FinanceSnapshot(new DateOnly(2026, 1, 1), [new AccountBalance(new Account(1, "Primary", new AccountType(1, "IRA", new AccountTypeGroup(1, "Investments"))), 100m)])));

            var operation = CreateOperation(institutionService.Object, groupService.Object, typeService.Object, accountService.Object, snapshotService.Object);

            await operation.ExecuteAsync(filePath, "shane", dryRun: false);

            institutionService.Verify(s => s.CreateInstitutionAsync(It.IsAny<CreateInstitutionCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            snapshotService.Verify(s => s.CreateSnapshotAsync(It.IsAny<CreateFinanceSnapshotCommand>(), It.IsAny<CancellationToken>()), Times.Once);
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
