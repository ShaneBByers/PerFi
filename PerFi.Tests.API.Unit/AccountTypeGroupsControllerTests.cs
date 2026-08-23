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

public sealed class AccountTypeGroupsControllerTests
{
    private static AccountTypeGroupsController CreateController(RecordingAccountTypeGroupService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingAccountTypeGroupService { GroupToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var controller = CreateController(new RecordingAccountTypeGroupService { GroupToReturn = new AccountTypeGroup(1, "Assets") });

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerFi.API.Responses.AccountTypeGroupResponse>(ok.Value);
        Assert.Equal("Assets", response.Name);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingAccountTypeGroupService());

        var result = await controller.Create(new CreateAccountTypeGroupRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new RecordingAccountTypeGroupService { CreateResult = Result<AccountTypeGroup>.Failure("nope") };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateAccountTypeGroupRequest("Assets"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingAccountTypeGroupService { CreateResult = Result<AccountTypeGroup>.Success(new AccountTypeGroup(1, "Assets")) };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateAccountTypeGroupRequest("Assets"));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AccountTypeGroupsController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingAccountTypeGroupService());

        var result = await controller.Update(1, new UpdateAccountTypeGroupRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingAccountTypeGroupService { UpdateResult = Result.Failure("Account type group with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateAccountTypeGroupRequest("Assets"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingAccountTypeGroupService { UpdateResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateAccountTypeGroupRequest("Assets"));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingAccountTypeGroupService { DeleteResult = Result.Failure("Account type group with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingAccountTypeGroupService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    private sealed class RecordingAccountTypeGroupService : IAccountTypeGroupService
    {
        public AccountTypeGroup? GroupToReturn { get; set; }
        public Result<AccountTypeGroup> CreateResult { get; set; } = Result<AccountTypeGroup>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<AccountTypeGroup>> GetAllAccountTypeGroupsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AccountTypeGroup>>([]);

        public Task<AccountTypeGroup?> GetAccountTypeGroupByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(GroupToReturn);

        public Task<Result<AccountTypeGroup>> CreateAccountTypeGroupAsync(CreateAccountTypeGroupCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateAccountTypeGroupAsync(UpdateAccountTypeGroupCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteAccountTypeGroupAsync(int accountTypeGroupId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);

        public Task<Result> ReorderAccountTypeGroupsAsync(ReorderAccountTypeGroupCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }
}
