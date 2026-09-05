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

public sealed class SalaryProgressionsControllerTests
{
    private static SalaryProgressionsController CreateController(RecordingSalaryProgressionService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = CreateController(new RecordingSalaryProgressionService());

        var result = await controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingSalaryProgressionService { EntryToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingSalaryProgressionService());

        var result = await controller.Create(new CreateSalaryProgressionRequest(default, -1m));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingSalaryProgressionService { CreateResult = Result<SalaryProgression>.Success(new SalaryProgression(5, new DateOnly(2026, 1, 1), 100_000m)) };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateSalaryProgressionRequest(new DateOnly(2026, 1, 1), 100_000m));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(SalaryProgressionsController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingSalaryProgressionService { UpdateResult = Result.Failure("Salary progression with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateSalaryProgressionRequest(new DateOnly(2026, 1, 1), 100_000m));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingSalaryProgressionService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    private sealed class RecordingSalaryProgressionService : ISalaryProgressionService
    {
        public SalaryProgression? EntryToReturn { get; set; } = new(1, new DateOnly(2026, 1, 1), 100_000m);
        public Result<SalaryProgression> CreateResult { get; set; } = Result<SalaryProgression>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<SalaryProgression>> GetAllSalaryProgressionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SalaryProgression>>([EntryToReturn ?? new SalaryProgression(1, new DateOnly(2026, 1, 1), 100_000m)]);

        public Task<SalaryProgression?> GetSalaryProgressionByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(EntryToReturn);

        public Task<Result<SalaryProgression>> CreateSalaryProgressionAsync(CreateSalaryProgressionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateSalaryProgressionAsync(UpdateSalaryProgressionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteSalaryProgressionAsync(int salaryProgressionId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);
    }
}
