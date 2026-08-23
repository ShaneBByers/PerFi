using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Controllers;
using PerFi.API.Requests;
using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.API.Unit;

public sealed class SnapshotsControllerTests
{
    private static SnapshotsController CreateController(RecordingFinanceSnapshotService snapshotService, RecordingInstitutionService? institutionService = null)
        => new(snapshotService, institutionService ?? new RecordingInstitutionService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task Create_WithNegativeBalance_PassesBalanceThroughToService()
    {
        var snapshotService = new RecordingFinanceSnapshotService();
        var controller = CreateController(snapshotService);

        var request = new CreateFinanceSnapshotRequest(
            new DateOnly(2026, 8, 9),
            new Dictionary<int, decimal> { [42] = -125.50m });

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(SnapshotsController.Get), created.ActionName);
        Assert.NotNull(snapshotService.LastCreateCommand);
        Assert.Equal(new DateOnly(2026, 8, 9), snapshotService.LastCreateCommand!.SnapshotDate);
        Assert.True(snapshotService.LastCreateCommand.AccountIdToBalanceMap.TryGetValue(42, out var balance));
        Assert.Equal(-125.50m, balance);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingFinanceSnapshotService());

        var result = await controller.Create(new CreateFinanceSnapshotRequest(default, new Dictionary<int, decimal>()));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var snapshotService = new RecordingFinanceSnapshotService { CreateResult = Result<FinanceSnapshot>.Failure("nope") };
        var controller = CreateController(snapshotService);

        var result = await controller.Create(new CreateFinanceSnapshotRequest(new DateOnly(2026, 8, 9), new Dictionary<int, decimal> { [1] = 10m }));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingFinanceSnapshotService { SnapshotToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var snapshotService = new RecordingFinanceSnapshotService { SnapshotToReturn = BuildSnapshot() };
        var controller = CreateController(snapshotService);

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<PerFi.API.Responses.FinanceSnapshotResponse>(ok.Value);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingFinanceSnapshotService());

        var result = await controller.Update(1, new UpdateFinanceSnapshotRequest(default, new Dictionary<int, decimal>()));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var snapshotService = new RecordingFinanceSnapshotService { UpdateResult = Result.Failure("Snapshot with ID '1' not found.") };
        var controller = CreateController(snapshotService);

        var result = await controller.Update(1, new UpdateFinanceSnapshotRequest(new DateOnly(2026, 8, 9), new Dictionary<int, decimal> { [1] = 10m }));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var snapshotService = new RecordingFinanceSnapshotService { UpdateResult = Result.Success() };
        var controller = CreateController(snapshotService);

        var result = await controller.Update(1, new UpdateFinanceSnapshotRequest(new DateOnly(2026, 8, 9), new Dictionary<int, decimal> { [1] = 10m }));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task BulkUpdateCells_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingFinanceSnapshotService());

        var result = await controller.BulkUpdateCells(new BulkUpdateFinanceSnapshotCellsRequest([]));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BulkUpdateCells_WhenSuccessful_ReturnsNoContent()
    {
        var snapshotService = new RecordingFinanceSnapshotService { UpdateCellsResult = Result.Success() };
        var controller = CreateController(snapshotService);

        var result = await controller.BulkUpdateCells(new BulkUpdateFinanceSnapshotCellsRequest([new SnapshotCellUpdateRequest(1, 1, 10m)]));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var snapshotService = new RecordingFinanceSnapshotService { DeleteResult = Result.Failure("Snapshot with ID '1' not found.") };
        var controller = CreateController(snapshotService);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var snapshotService = new RecordingFinanceSnapshotService { DeleteResult = Result.Success() };
        var controller = CreateController(snapshotService);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    private static FinanceSnapshot BuildSnapshot()
        => new(1, new DateOnly(2026, 8, 9), [CreateBalance(new Dictionary<int, decimal> { [1] = 10m })]);

    private sealed class RecordingFinanceSnapshotService : IFinanceSnapshotService
    {
        public CreateFinanceSnapshotCommand? LastCreateCommand { get; private set; }
        public FinanceSnapshot? SnapshotToReturn { get; set; }
        public Result<FinanceSnapshot>? CreateResult { get; set; }
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result UpdateCellsResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FinanceSnapshot>>([]);

        public Task<FinanceSnapshot?> GetSnapshotByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(SnapshotToReturn);

        public Task<Result<FinanceSnapshot>> CreateSnapshotAsync(CreateFinanceSnapshotCommand command, CancellationToken cancellationToken = default)
        {
            LastCreateCommand = command;

            if (CreateResult is not null)
                return Task.FromResult(CreateResult);

            return Task.FromResult(Result<FinanceSnapshot>.Success(new FinanceSnapshot(command.SnapshotDate, [CreateBalance(command.AccountIdToBalanceMap)])));
        }

        public Task<Result> UpdateSnapshotAsync(UpdateFinanceSnapshotCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> UpdateSnapshotCellsAsync(BulkUpdateFinanceSnapshotCellsCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateCellsResult);

        public Task<Result> DeleteSnapshotAsync(int snapshotId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);
    }

    private static AccountBalance CreateBalance(IReadOnlyDictionary<int, decimal> accountIdToBalanceMap)
    {
        var accountId = accountIdToBalanceMap.Keys.First();
        var balance = accountIdToBalanceMap[accountId];
        var account = new Account(accountId, "Test Account", new AccountType("Test Type", new AccountTypeGroup("Investments")), 1);

        return new AccountBalance(account, balance);
    }

    private sealed class RecordingInstitutionService : IInstitutionService
    {
        public Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Institution>>([]);

        public Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Institution?>(null);

        public Task<Result<Institution>> CreateInstitutionAsync(CreateInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Institution>.Failure("Not implemented in test."));

        public Task<Result> UpdateInstitutionAsync(UpdateInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> DeleteInstitutionAsync(int institutionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> ReorderInstitutionsAsync(ReorderInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }
}
