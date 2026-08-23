using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class FinanceSnapshotServiceTests
{
    private static Account CreateAccount(int id) => new(id, "Checking", new AccountType("Checking", new AccountTypeGroup("Assets")), 1);

    private static Mock<IAccountRepository> CreateAccountRepoReturning(params int[] validAccountIds)
    {
        var accountRepo = new Mock<IAccountRepository>();
        foreach (var id in validAccountIds)
        {
            accountRepo.Setup(r => r.GetAccountByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccount(id));
        }

        return accountRepo;
    }

    [Fact]
    public async Task GetAllSnapshotsAsync_DelegatesToRepository()
    {
        var repo = new Mock<IFinanceSnapshotRepository>();
        var snapshots = new List<FinanceSnapshot> { new(new DateOnly(2026, 1, 1), [new AccountBalance(CreateAccount(1), 5m)]) };
        repo.Setup(r => r.GetAllSnapshotsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snapshots);

        var service = new FinanceSnapshotService(repo.Object, Mock.Of<IAccountRepository>());

        var result = await service.GetAllSnapshotsAsync();

        Assert.Same(snapshots, result);
    }

    [Fact]
    public async Task GetSnapshotByIdAsync_DelegatesToRepository()
    {
        var repo = new Mock<IFinanceSnapshotRepository>();
        repo.Setup(r => r.GetSnapshotByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync((FinanceSnapshot?)null);

        var service = new FinanceSnapshotService(repo.Object, Mock.Of<IAccountRepository>());

        var result = await service.GetSnapshotByIdAsync(4);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new FinanceSnapshotService(Mock.Of<IFinanceSnapshotRepository>(), Mock.Of<IAccountRepository>());

        var result = await service.CreateSnapshotAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithEmptyBalanceMap_ReturnsFailure()
    {
        var service = new FinanceSnapshotService(Mock.Of<IFinanceSnapshotRepository>(), Mock.Of<IAccountRepository>());

        var result = await service.CreateSnapshotAsync(new CreateFinanceSnapshotCommand(new DateOnly(2026, 1, 1), new Dictionary<int, decimal>()));

        Assert.True(result.IsFailure);
        Assert.Contains("at least one account balance", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithUnknownAccount_ReturnsFailure()
    {
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetAccountByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        var service = new FinanceSnapshotService(Mock.Of<IFinanceSnapshotRepository>(), accountRepo.Object);

        var result = await service.CreateSnapshotAsync(new CreateFinanceSnapshotCommand(new DateOnly(2026, 1, 1), new Dictionary<int, decimal> { [99] = 10m }));

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithDefaultDate_ReturnsFailureFromArgumentException()
    {
        var accountRepo = CreateAccountRepoReturning(1);

        var service = new FinanceSnapshotService(Mock.Of<IFinanceSnapshotRepository>(), accountRepo.Object);

        var result = await service.CreateSnapshotAsync(new CreateFinanceSnapshotCommand(default, new Dictionary<int, decimal> { [1] = 10m }));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WhenRepositoryFails_ReturnsFailure()
    {
        var accountRepo = CreateAccountRepoReturning(1);
        var snapshotRepo = new Mock<IFinanceSnapshotRepository>();
        snapshotRepo.Setup(r => r.AddSnapshotAsync(It.IsAny<FinanceSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure("db error"));

        var service = new FinanceSnapshotService(snapshotRepo.Object, accountRepo.Object);

        var result = await service.CreateSnapshotAsync(new CreateFinanceSnapshotCommand(new DateOnly(2026, 1, 1), new Dictionary<int, decimal> { [1] = 10m }));

        Assert.True(result.IsFailure);
        Assert.Equal("db error", result.Error);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var accountRepo = CreateAccountRepoReturning(1);
        var snapshotRepo = new Mock<IFinanceSnapshotRepository>();
        snapshotRepo.Setup(r => r.AddSnapshotAsync(It.IsAny<FinanceSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(21));

        var service = new FinanceSnapshotService(snapshotRepo.Object, accountRepo.Object);

        var result = await service.CreateSnapshotAsync(new CreateFinanceSnapshotCommand(new DateOnly(2026, 1, 1), new Dictionary<int, decimal> { [1] = 10m }));

        Assert.True(result.IsSuccess);
        Assert.Equal(21, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateSnapshotAsync_WithMissingSnapshot_ReturnsFailure()
    {
        var snapshotRepo = new Mock<IFinanceSnapshotRepository>();
        snapshotRepo.Setup(r => r.GetSnapshotByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((FinanceSnapshot?)null);

        var service = new FinanceSnapshotService(snapshotRepo.Object, Mock.Of<IAccountRepository>());

        var result = await service.UpdateSnapshotAsync(new UpdateFinanceSnapshotCommand(1, new DateOnly(2026, 1, 1), new Dictionary<int, decimal> { [1] = 10m }));

        Assert.True(result.IsFailure);
        Assert.Contains("Snapshot with ID", result.Error);
    }

    [Fact]
    public async Task UpdateSnapshotAsync_WithUnknownAccount_ReturnsFailure()
    {
        var existing = new FinanceSnapshot(1, new DateOnly(2026, 1, 1), [new AccountBalance(CreateAccount(1), 5m)]);
        var snapshotRepo = new Mock<IFinanceSnapshotRepository>();
        snapshotRepo.Setup(r => r.GetSnapshotByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetAccountByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        var service = new FinanceSnapshotService(snapshotRepo.Object, accountRepo.Object);

        var result = await service.UpdateSnapshotAsync(new UpdateFinanceSnapshotCommand(1, new DateOnly(2026, 1, 1), new Dictionary<int, decimal> { [99] = 10m }));

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public async Task UpdateSnapshotAsync_WithValidCommand_ReturnsSuccess()
    {
        var existing = new FinanceSnapshot(1, new DateOnly(2026, 1, 1), [new AccountBalance(CreateAccount(1), 5m)]);
        var snapshotRepo = new Mock<IFinanceSnapshotRepository>();
        snapshotRepo.Setup(r => r.GetSnapshotByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        snapshotRepo.Setup(r => r.UpdateSnapshotAsync(It.IsAny<FinanceSnapshot>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var accountRepo = CreateAccountRepoReturning(1);

        var service = new FinanceSnapshotService(snapshotRepo.Object, accountRepo.Object);

        var result = await service.UpdateSnapshotAsync(new UpdateFinanceSnapshotCommand(1, new DateOnly(2026, 1, 1), new Dictionary<int, decimal> { [1] = 20m }));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateSnapshotCellsAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new FinanceSnapshotService(Mock.Of<IFinanceSnapshotRepository>(), Mock.Of<IAccountRepository>());

        var result = await service.UpdateSnapshotCellsAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateSnapshotCellsAsync_WithNoUpdates_ReturnsFailure()
    {
        var service = new FinanceSnapshotService(Mock.Of<IFinanceSnapshotRepository>(), Mock.Of<IAccountRepository>());

        var result = await service.UpdateSnapshotCellsAsync(new BulkUpdateFinanceSnapshotCellsCommand([]));

        Assert.True(result.IsFailure);
        Assert.Contains("at least one cell update", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateSnapshotCellsAsync_WithNonPositiveSnapshotId_ReturnsFailure()
    {
        var service = new FinanceSnapshotService(Mock.Of<IFinanceSnapshotRepository>(), Mock.Of<IAccountRepository>());

        var result = await service.UpdateSnapshotCellsAsync(new BulkUpdateFinanceSnapshotCellsCommand(
            [new SnapshotCellUpdateCommand(0, 1, 10m)]));

        Assert.True(result.IsFailure);
        Assert.Contains("snapshot IDs", result.Error);
    }

    [Fact]
    public async Task UpdateSnapshotCellsAsync_WithNonPositiveAccountId_ReturnsFailure()
    {
        var service = new FinanceSnapshotService(Mock.Of<IFinanceSnapshotRepository>(), Mock.Of<IAccountRepository>());

        var result = await service.UpdateSnapshotCellsAsync(new BulkUpdateFinanceSnapshotCellsCommand(
            [new SnapshotCellUpdateCommand(1, 0, 10m)]));

        Assert.True(result.IsFailure);
        Assert.Contains("account IDs", result.Error);
    }

    [Fact]
    public async Task UpdateSnapshotCellsAsync_WithDuplicateCellUpdates_KeepsLastValue()
    {
        IReadOnlyList<PerFi.Domain.Entities.SnapshotCellUpdate>? capturedUpdates = null;

        var snapshotRepo = new Mock<IFinanceSnapshotRepository>();
        snapshotRepo.Setup(r => r.UpdateSnapshotCellsAsync(It.IsAny<IReadOnlyList<PerFi.Domain.Entities.SnapshotCellUpdate>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<PerFi.Domain.Entities.SnapshotCellUpdate>, CancellationToken>((updates, _) => capturedUpdates = updates)
            .ReturnsAsync(Result.Success());

        var service = new FinanceSnapshotService(snapshotRepo.Object, Mock.Of<IAccountRepository>());

        var result = await service.UpdateSnapshotCellsAsync(new BulkUpdateFinanceSnapshotCellsCommand(
            [
                new SnapshotCellUpdateCommand(1, 1, 10m),
                new SnapshotCellUpdateCommand(1, 1, 99m)
            ]));

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedUpdates);
        var update = Assert.Single(capturedUpdates!);
        Assert.Equal(99m, update.Balance);
    }

    [Fact]
    public async Task DeleteSnapshotAsync_DelegatesToRepository()
    {
        var repo = new Mock<IFinanceSnapshotRepository>();
        repo.Setup(r => r.DeleteSnapshotAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new FinanceSnapshotService(repo.Object, Mock.Of<IAccountRepository>());

        var result = await service.DeleteSnapshotAsync(1);

        Assert.True(result.IsSuccess);
    }
}
