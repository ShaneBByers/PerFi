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

public sealed class ContributionContributorsControllerTests
{
    private static ContributionContributorsController CreateController(RecordingContributionContributorService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task GetAll_PreservesDisplayOrderInResponse()
    {
        var controller = CreateController(new RecordingContributionContributorService());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.ContributionContributorResponse>>(ok.Value);
        var contributor = Assert.Single(response);

        Assert.Equal(7, contributor.DisplayOrder);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingContributionContributorService { ContributorToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var controller = CreateController(new RecordingContributionContributorService { ContributorToReturn = new ContributionContributor(1, "Alice") });

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerFi.API.Responses.ContributionContributorResponse>(ok.Value);
        Assert.Equal("Alice", response.Name);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingContributionContributorService());

        var result = await controller.Create(new CreateContributionContributorRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new RecordingContributionContributorService { CreateResult = Result<ContributionContributor>.Failure("nope") };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateContributionContributorRequest("Alice"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingContributionContributorService { CreateResult = Result<ContributionContributor>.Success(new ContributionContributor(5, "Alice")) };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateContributionContributorRequest("Alice"));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ContributionContributorsController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingContributionContributorService());

        var result = await controller.Update(1, new UpdateContributionContributorRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingContributionContributorService { UpdateResult = Result.Failure("Contribution contributor with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateContributionContributorRequest("Alice"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingContributionContributorService { UpdateResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateContributionContributorRequest("Alice"));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingContributionContributorService { DeleteResult = Result.Failure("Contribution contributor with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingContributionContributorService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Reorder_PassesOrderedIdsToService()
    {
        var service = new RecordingContributionContributorService();
        var controller = CreateController(service);

        var result = await controller.Reorder(new ReorderContributionContributorsRequest([3, 1, 2]));

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(service.LastReorderCommand);
        Assert.Equal([3, 1, 2], service.LastReorderCommand!.OrderedContributionContributorIds);
    }

    private sealed class RecordingContributionContributorService : IContributionContributorService
    {
        public ReorderContributionContributorsCommand? LastReorderCommand { get; private set; }
        public ContributionContributor? ContributorToReturn { get; set; } = new ContributionContributor(1, "Alice") { DisplayOrder = 7 };
        public Result<ContributionContributor> CreateResult { get; set; } = Result<ContributionContributor>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<ContributionContributor>> GetAllContributionContributorsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ContributionContributor>>([new ContributionContributor(1, "Alice") { DisplayOrder = 7 }]);

        public Task<ContributionContributor?> GetContributionContributorByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(ContributorToReturn);

        public Task<Result<ContributionContributor>> CreateContributionContributorAsync(CreateContributionContributorCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateContributionContributorAsync(UpdateContributionContributorCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteContributionContributorAsync(int contributionContributorId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);

        public Task<Result> ReorderContributionContributorsAsync(ReorderContributionContributorsCommand command, CancellationToken cancellationToken = default)
        {
            LastReorderCommand = command;
            return Task.FromResult(Result.Success());
        }
    }
}