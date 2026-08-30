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

public sealed class TransactionCategoryGroupsControllerTests
{
    private static TransactionCategoryGroupsController CreateController(RecordingTransactionCategoryGroupService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task GetAll_PreservesDisplayOrderInResponse()
    {
        var controller = CreateController(new RecordingTransactionCategoryGroupService());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.TransactionCategoryGroupResponse>>(ok.Value);
        var group = Assert.Single(response);

        Assert.Equal(7, group.DisplayOrder);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingTransactionCategoryGroupService { GroupToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var controller = CreateController(new RecordingTransactionCategoryGroupService { GroupToReturn = new TransactionCategoryGroup(1, "Assets") });

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerFi.API.Responses.TransactionCategoryGroupResponse>(ok.Value);
        Assert.Equal("Assets", response.Name);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingTransactionCategoryGroupService());

        var result = await controller.Create(new CreateTransactionCategoryGroupRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new RecordingTransactionCategoryGroupService { CreateResult = Result<TransactionCategoryGroup>.Failure("nope") };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateTransactionCategoryGroupRequest("Assets"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingTransactionCategoryGroupService { CreateResult = Result<TransactionCategoryGroup>.Success(new TransactionCategoryGroup(5, "Assets")) };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateTransactionCategoryGroupRequest("Assets"));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TransactionCategoryGroupsController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingTransactionCategoryGroupService());

        var result = await controller.Update(1, new UpdateTransactionCategoryGroupRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingTransactionCategoryGroupService { UpdateResult = Result.Failure("Transaction category group with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateTransactionCategoryGroupRequest("Assets"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingTransactionCategoryGroupService { UpdateResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateTransactionCategoryGroupRequest("Assets"));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingTransactionCategoryGroupService { DeleteResult = Result.Failure("Transaction category group with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingTransactionCategoryGroupService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Reorder_PassesOrderedIdsToService()
    {
        var service = new RecordingTransactionCategoryGroupService();
        var controller = CreateController(service);

        var result = await controller.Reorder(new ReorderTransactionCategoryGroupsRequest([3, 1, 2]));

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(service.LastReorderCommand);
        Assert.Equal([3, 1, 2], service.LastReorderCommand!.OrderedTransactionCategoryGroupIds);
    }

    private sealed class RecordingTransactionCategoryGroupService : ITransactionCategoryGroupService
    {
        public ReorderTransactionCategoryGroupsCommand? LastReorderCommand { get; private set; }
        public TransactionCategoryGroup? GroupToReturn { get; set; } = new TransactionCategoryGroup(1, "Assets") { DisplayOrder = 7 };
        public Result<TransactionCategoryGroup> CreateResult { get; set; } = Result<TransactionCategoryGroup>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<TransactionCategoryGroup>> GetAllTransactionCategoryGroupsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TransactionCategoryGroup>>([new TransactionCategoryGroup(1, "Assets") { DisplayOrder = 7 }]);

        public Task<TransactionCategoryGroup?> GetTransactionCategoryGroupByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(GroupToReturn);

        public Task<Result<TransactionCategoryGroup>> CreateTransactionCategoryGroupAsync(CreateTransactionCategoryGroupCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateTransactionCategoryGroupAsync(UpdateTransactionCategoryGroupCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteTransactionCategoryGroupAsync(int transactionCategoryGroupId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);

        public Task<Result> ReorderTransactionCategoryGroupsAsync(ReorderTransactionCategoryGroupsCommand command, CancellationToken cancellationToken = default)
        {
            LastReorderCommand = command;
            return Task.FromResult(Result.Success());
        }
    }
}