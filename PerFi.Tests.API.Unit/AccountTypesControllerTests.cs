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

public sealed class AccountTypesControllerTests
{
    private static AccountTypesController CreateController(RecordingAccountTypeService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static AccountType BuildAccountType() => new(1, "Checking", new AccountTypeGroup(1, "Assets"));

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingAccountTypeService { TypeToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var controller = CreateController(new RecordingAccountTypeService { TypeToReturn = BuildAccountType() });

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerFi.API.Responses.AccountTypeResponse>(ok.Value);
        Assert.Equal("Checking", response.Name);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingAccountTypeService());

        var result = await controller.Create(new CreateAccountTypeRequest("   ", 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new RecordingAccountTypeService { CreateResult = Result<AccountType>.Failure("nope") };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateAccountTypeRequest("Checking", 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingAccountTypeService { CreateResult = Result<AccountType>.Success(BuildAccountType()) };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateAccountTypeRequest("Checking", 1));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AccountTypesController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingAccountTypeService());

        var result = await controller.Update(1, new UpdateAccountTypeRequest("   ", 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingAccountTypeService { UpdateResult = Result.Failure("Account type group with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateAccountTypeRequest("Checking", 1));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingAccountTypeService { UpdateResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateAccountTypeRequest("Checking", 1));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingAccountTypeService { DeleteResult = Result.Failure("Account type with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingAccountTypeService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    private sealed class RecordingAccountTypeService : IAccountTypeService
    {
        public AccountType? TypeToReturn { get; set; }
        public Result<AccountType> CreateResult { get; set; } = Result<AccountType>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<AccountType>> GetAllAccountTypesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AccountType>>([]);

        public Task<AccountType?> GetAccountTypeByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(TypeToReturn);

        public Task<Result<AccountType>> CreateAccountTypeAsync(CreateAccountTypeCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateAccountTypeAsync(UpdateAccountTypeCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteAccountTypeAsync(int accountTypeId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);

        public Task<Result> ReorderAccountTypesAsync(ReorderAccountTypeCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }
}
