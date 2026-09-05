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

public sealed class UserConfigurationControllerTests
{
    private static UserConfigurationController CreateController(RecordingUserConfigurationService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static CreateUserConfigurationRequest CreateRequest()
        => new(new DateOnly(1990, 1, 1), PayCycleType.BiWeekly, new DateOnly(2026, 1, 2), 0.03m, 0.025m);

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingUserConfigurationService { ConfigurationToReturn = null });

        var result = await controller.Get();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var controller = CreateController(new RecordingUserConfigurationService());

        var result = await controller.Get();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingUserConfigurationService());

        var result = await controller.Create(new CreateUserConfigurationRequest(default, PayCycleType.BiWeekly, default, 0m, 0m));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingUserConfigurationService
        {
            CreateResult = Result<UserConfiguration>.Success(new UserConfiguration(1, new DateOnly(1990, 1, 1), PayCycleType.BiWeekly, new DateOnly(2026, 1, 2), 0.03m, 0.025m))
        };
        var controller = CreateController(service);

        var result = await controller.Create(CreateRequest());

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingUserConfigurationService { UpdateResult = Result.Failure("User configuration with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateUserConfigurationRequest(new DateOnly(1990, 1, 1), PayCycleType.BiWeekly, new DateOnly(2026, 1, 2), 0.03m, 0.025m));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingUserConfigurationService { UpdateResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateUserConfigurationRequest(new DateOnly(1990, 1, 1), PayCycleType.BiWeekly, new DateOnly(2026, 1, 2), 0.03m, 0.025m));

        Assert.IsType<NoContentResult>(result);
    }

    private sealed class RecordingUserConfigurationService : IUserConfigurationService
    {
        public UserConfiguration? ConfigurationToReturn { get; set; } = new(1, new DateOnly(1990, 1, 1), PayCycleType.BiWeekly, new DateOnly(2026, 1, 2), 0.03m, 0.025m);
        public Result<UserConfiguration> CreateResult { get; set; } = Result<UserConfiguration>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<UserConfiguration?> GetUserConfigurationAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ConfigurationToReturn);

        public Task<Result<UserConfiguration>> CreateUserConfigurationAsync(CreateUserConfigurationCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateUserConfigurationAsync(UpdateUserConfigurationCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);
    }
}
