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

public sealed class ImportTransactionsCsvOperationTests : IDisposable
{
    private readonly Microsoft.AspNetCore.Identity.UserManager<PerFi.Infrastructure.Entities.ApplicationUser> _userManager;
    private readonly PerFi.Infrastructure.PerFiDbContext _dbContext;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly ConsoleCurrentUserService _currentUser = new();

    public ImportTransactionsCsvOperationTests()
    {
        (_userManager, _dbContext, _connection) = IdentityTestHelper.CreateUserManager();
    }

    private ImportTransactionsCsvOperation CreateOperation(
        IInstitutionService? institutionService = null,
        ITransactionCategoryGroupService? transactionCategoryGroupService = null,
        ITransactionCategoryService? transactionCategoryService = null,
        ITransactionService? transactionService = null)
        => new(
            _dbContext,
            new TransactionCsvParser(),
            _userManager,
            _currentUser,
            institutionService ?? Mock.Of<IInstitutionService>(),
            transactionCategoryGroupService ?? Mock.Of<ITransactionCategoryGroupService>(),
            transactionCategoryService ?? Mock.Of<ITransactionCategoryService>(),
            transactionService ?? MockEmptyTransactionService());

    private static Mock<ITransactionService> MockTransactionServiceMock()
    {
        var mock = new Mock<ITransactionService>();
        mock.Setup(s => s.GetAllTransactionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Transaction>)[]);
        return mock;
    }

    private static ITransactionService MockEmptyTransactionService() => MockTransactionServiceMock().Object;

    private static string CreateTempCsv(string csv)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"perfi-import-transactions-{Guid.NewGuid():N}.csv");
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
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
2026-01-02,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,-$1.00,
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
    public async Task ExecuteAsync_WithExistingTransactions_ThrowsInvalidOperationException()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var filePath = CreateTempCsv("""
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
2026-01-02,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,-$1.00,
""");

        try
        {
            var institutionService = new Mock<IInstitutionService>();
            institutionService.Setup(s => s.GetAllInstitutionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Institution>)[
                    new Institution(1, "Chase", [new Account(1, "Chase Primary Checking", new AccountType(1, "Checking", new AccountTypeGroup(1, "Checking & Savings")))])
                ]);

            var transactionService = new Mock<ITransactionService>();
            transactionService.Setup(s => s.GetAllTransactionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Transaction>)[
                    new Transaction(1, new DateOnly(2026, 1, 1), "Existing", -1m, new TransactionCategory(1, "Other", new TransactionCategoryGroup(1, "Expenses")), 1)
                ]);

            var operation = CreateOperation(institutionService: institutionService.Object, transactionService: transactionService.Object);

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
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
2026-01-02,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,-$1.00,
""");

        try
        {
            var institutionService = new Mock<IInstitutionService>();
            institutionService.Setup(s => s.GetAllInstitutionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Institution>)[
                    new Institution(1, "Chase", [new Account(1, "Chase Primary Checking", new AccountType(1, "Checking", new AccountTypeGroup(1, "Checking & Savings")))])
                ]);

            var categoryGroupService = new Mock<ITransactionCategoryGroupService>();
            var operation = CreateOperation(institutionService: institutionService.Object, transactionCategoryGroupService: categoryGroupService.Object);

            await operation.ExecuteAsync(filePath, "shane", dryRun: true);

            categoryGroupService.Verify(s => s.CreateTransactionCategoryGroupAsync(It.IsAny<CreateTransactionCategoryGroupCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NotDryRun_WithValidCsv_CreatesCategoryGroupCategoryAndTransaction()
    {
        await _userManager.CreateAsync(new PerFi.Infrastructure.Entities.ApplicationUser { UserName = "shane" }, "Test-Password1!");
        var filePath = CreateTempCsv("""
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
2026-01-02,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,-$1.00,
""");

        try
        {
            var institutionService = new Mock<IInstitutionService>();
            institutionService.Setup(s => s.GetAllInstitutionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Institution>)[
                    new Institution(1, "Chase", [new Account(1, "Chase Primary Checking", new AccountType(1, "Checking", new AccountTypeGroup(1, "Checking & Savings")))])
                ]);

            var categoryGroupService = new Mock<ITransactionCategoryGroupService>();
            categoryGroupService.Setup(s => s.GetAllTransactionCategoryGroupsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<TransactionCategoryGroup>)[]);
            categoryGroupService.Setup(s => s.CreateTransactionCategoryGroupAsync(It.IsAny<CreateTransactionCategoryGroupCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TransactionCategoryGroup>.Success(new TransactionCategoryGroup(1, "Expenses (Required)")));

            var categoryService = new Mock<ITransactionCategoryService>();
            categoryService.Setup(s => s.GetAllTransactionCategoriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<TransactionCategory>)[]);
            categoryService.Setup(s => s.CreateTransactionCategoryAsync(It.IsAny<CreateTransactionCategoryCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TransactionCategory>.Success(new TransactionCategory(1, "Mortgage", new TransactionCategoryGroup(1, "Expenses (Required)"))));

            var transactionService = new Mock<ITransactionService>();
            transactionService.Setup(s => s.GetAllTransactionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Transaction>)[]);
            transactionService.Setup(s => s.CreateTransactionAsync(It.IsAny<CreateTransactionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Transaction>.Success(new Transaction(1, new DateOnly(2026, 1, 2), "Chase Home", -1m, new TransactionCategory(1, "Mortgage", new TransactionCategoryGroup(1, "Expenses (Required)")), 1)));

            var operation = CreateOperation(
                institutionService.Object,
                categoryGroupService.Object,
                categoryService.Object,
                transactionService.Object);

            await operation.ExecuteAsync(filePath, "shane", dryRun: false);

            categoryGroupService.Verify(s => s.CreateTransactionCategoryGroupAsync(It.IsAny<CreateTransactionCategoryGroupCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            categoryService.Verify(s => s.CreateTransactionCategoryAsync(It.IsAny<CreateTransactionCategoryCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            transactionService.Verify(s => s.CreateTransactionAsync(It.IsAny<CreateTransactionCommand>(), It.IsAny<CancellationToken>()), Times.Once);
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
