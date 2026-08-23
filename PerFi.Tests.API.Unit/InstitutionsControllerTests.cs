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

public sealed class InstitutionsControllerTests
{
    private static InstitutionsController CreateController(RecordingInstitutionService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingInstitutionService { InstitutionToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var controller = CreateController(new RecordingInstitutionService { InstitutionToReturn = new Institution(1, "First Bank", []) });

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerFi.API.Responses.InstitutionResponse>(ok.Value);
        Assert.Equal("First Bank", response.Name);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingInstitutionService());

        var result = await controller.Create(new CreateInstitutionRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new RecordingInstitutionService { CreateResult = Result<Institution>.Failure("nope") };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateInstitutionRequest("First Bank"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingInstitutionService { CreateResult = Result<Institution>.Success(new Institution(1, "First Bank", [])) };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateInstitutionRequest("First Bank"));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(InstitutionsController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingInstitutionService());

        var result = await controller.Update(1, new UpdateInstitutionRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingInstitutionService { UpdateResult = Result.Failure("Institution with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateInstitutionRequest("First Bank"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingInstitutionService { UpdateResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateInstitutionRequest("First Bank"));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingInstitutionService { DeleteResult = Result.Failure("Institution with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingInstitutionService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    private sealed class RecordingInstitutionService : IInstitutionService
    {
        public Institution? InstitutionToReturn { get; set; }
        public Result<Institution> CreateResult { get; set; } = Result<Institution>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Institution>>([]);

        public Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(InstitutionToReturn);

        public Task<Result<Institution>> CreateInstitutionAsync(CreateInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateInstitutionAsync(UpdateInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteInstitutionAsync(int institutionId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);

        public Task<Result> ReorderInstitutionsAsync(ReorderInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }
}
