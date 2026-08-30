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

public sealed class TransactionCategoriesControllerTests
{
    private static TransactionCategoriesController CreateController(RecordingTransactionCategoryService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task GetAll_PreservesDisplayOrderInResponse()
    {
        var controller = CreateController(new RecordingTransactionCategoryService());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.TransactionCategoryResponse>>(ok.Value);
        var category = Assert.Single(response);

        Assert.Equal(7, category.DisplayOrder);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingTransactionCategoryService { CategoryToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var controller = CreateController(new RecordingTransactionCategoryService { CategoryToReturn = BuildCategory() });

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerFi.API.Responses.TransactionCategoryResponse>(ok.Value);
        Assert.Equal("Groceries", response.Name);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingTransactionCategoryService());

        var result = await controller.Create(new CreateTransactionCategoryRequest("   ", 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new RecordingTransactionCategoryService { CreateResult = Result<TransactionCategory>.Failure("nope") };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateTransactionCategoryRequest("Groceries", 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingTransactionCategoryService { CreateResult = Result<TransactionCategory>.Success(BuildCategory(5)) };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateTransactionCategoryRequest("Groceries", 1));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TransactionCategoriesController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingTransactionCategoryService());

        var result = await controller.Update(1, new UpdateTransactionCategoryRequest("   ", 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingTransactionCategoryService { UpdateResult = Result.Failure("Transaction category with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateTransactionCategoryRequest("Groceries", 1));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingTransactionCategoryService { UpdateResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateTransactionCategoryRequest("Groceries", 1));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingTransactionCategoryService { DeleteResult = Result.Failure("Transaction category with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingTransactionCategoryService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Reorder_PassesOrderedIdsToService()
    {
        var service = new RecordingTransactionCategoryService();
        var controller = CreateController(service);

        var result = await controller.Reorder(new ReorderTransactionCategoriesRequest([3, 1, 2]));

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(service.LastReorderCommand);
        Assert.Equal([3, 1, 2], service.LastReorderCommand!.OrderedTransactionCategoryIds);
    }

    private static TransactionCategory BuildCategory(int id = 1)
        => new(id, "Groceries", new TransactionCategoryGroup(1, "Expenses")) { DisplayOrder = 7 };

    private sealed class RecordingTransactionCategoryService : ITransactionCategoryService
    {
        public ReorderTransactionCategoriesCommand? LastReorderCommand { get; private set; }
        public TransactionCategory? CategoryToReturn { get; set; } = BuildCategory();
        public Result<TransactionCategory> CreateResult { get; set; } = Result<TransactionCategory>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<TransactionCategory>> GetAllTransactionCategoriesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TransactionCategory>>([BuildCategory()]);

        public Task<TransactionCategory?> GetTransactionCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(CategoryToReturn);

        public Task<Result<TransactionCategory>> CreateTransactionCategoryAsync(CreateTransactionCategoryCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateTransactionCategoryAsync(UpdateTransactionCategoryCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteTransactionCategoryAsync(int transactionCategoryId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);

        public Task<Result> ReorderTransactionCategoriesAsync(ReorderTransactionCategoriesCommand command, CancellationToken cancellationToken = default)
        {
            LastReorderCommand = command;
            return Task.FromResult(Result.Success());
        }
    }
}