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

public sealed class AccountContributionPlansControllerTests
{
    private static AccountContributionPlansController CreateController(RecordingAccountContributionPlanService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static CreateAccountContributionPlanRequest CreateRequest()
        => new(ContributionContributorType.Self, new DateOnly(2024, 1, 1), 100m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = CreateController(new RecordingAccountContributionPlanService());

        var result = await controller.GetAll(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingAccountContributionPlanService { PlanToReturn = null });

        var result = await controller.Get(1, 1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingAccountContributionPlanService
        {
            CreateResult = Result<AccountContributionPlan>.Success(new AccountContributionPlan(5, 1, ContributionContributorType.Self, new DateOnly(2024, 1, 1), 100m, 0, 0, 0, 0, 0, 0, 0))
        };
        var controller = CreateController(service);

        var result = await controller.Create(1, CreateRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AccountContributionPlansController.Get), created.ActionName);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new RecordingAccountContributionPlanService { CreateResult = Result<AccountContributionPlan>.Failure("nope") };
        var controller = CreateController(service);

        var result = await controller.Create(1, CreateRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingAccountContributionPlanService { UpdateResult = Result.Failure("Account contribution plan with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, 1, new UpdateAccountContributionPlanRequest(ContributionContributorType.Self, new DateOnly(2024, 1, 1), 100m, 0m, 0m, 0m, 0m, 0m, 0m, 0m));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingAccountContributionPlanService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1, 1);

        Assert.IsType<NoContentResult>(result);
    }

    private sealed class RecordingAccountContributionPlanService : IAccountContributionPlanService
    {
        public AccountContributionPlan? PlanToReturn { get; set; } = new(1, 1, ContributionContributorType.Self, new DateOnly(2024, 1, 1), 100m, 0, 0, 0, 0, 0, 0, 0);
        public Result<AccountContributionPlan> CreateResult { get; set; } = Result<AccountContributionPlan>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<AccountContributionPlan>> GetAllByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AccountContributionPlan>>(PlanToReturn is null ? [] : [PlanToReturn]);

        public Task<AccountContributionPlan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(PlanToReturn);

        public Task<Result<AccountContributionPlan>> CreateAsync(CreateAccountContributionPlanCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateAsync(UpdateAccountContributionPlanCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteAsync(int accountContributionPlanId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);
    }
}
