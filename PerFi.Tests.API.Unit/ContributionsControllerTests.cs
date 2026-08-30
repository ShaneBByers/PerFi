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

public sealed class ContributionsControllerTests
{
    private static ContributionsController CreateController(RecordingContributionService contributionService, RecordingAccountService? accountService = null)
        => new(contributionService, accountService ?? new RecordingAccountService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task GetAll_UsesAccountNamesInResponse()
    {
        var controller = CreateController(new RecordingContributionService(), new RecordingAccountService());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.ContributionResponse>>(ok.Value);
        var contribution = Assert.Single(response);

        Assert.Equal("Test Account", contribution.Account.Name);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingContributionService { ContributionToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var controller = CreateController(new RecordingContributionService { ContributionToReturn = BuildContribution() });

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerFi.API.Responses.ContributionResponse>(ok.Value);
        Assert.Equal(25m, response.Amount);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingContributionService());

        var result = await controller.Create(new CreateContributionRequest(default, 0m, 1, 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new RecordingContributionService { CreateResult = Result<Contribution>.Failure("nope") };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateContributionRequest(new DateOnly(2026, 8, 9), 25m, 1, 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingContributionService { CreateResult = Result<Contribution>.Success(BuildContribution(5)) };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateContributionRequest(new DateOnly(2026, 8, 9), 25m, 1, 1));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ContributionsController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingContributionService());

        var result = await controller.Update(1, new UpdateContributionRequest(default, 0m, 1, 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingContributionService { UpdateResult = Result.Failure("Contribution with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateContributionRequest(new DateOnly(2026, 8, 9), 25m, 1, 1));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingContributionService { UpdateResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateContributionRequest(new DateOnly(2026, 8, 9), 25m, 1, 1));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingContributionService { DeleteResult = Result.Failure("Contribution with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingContributionService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    private static Contribution BuildContribution(int id = 1)
        => new(id, new DateOnly(2026, 8, 9), 25m, new ContributionContributor(1, "Alice"), 1);

    private sealed class RecordingContributionService : IContributionService
    {
        public Contribution? ContributionToReturn { get; set; } = BuildContribution();
        public Result<Contribution> CreateResult { get; set; } = Result<Contribution>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<Contribution>> GetAllContributionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Contribution>>([BuildContribution()]);

        public Task<Contribution?> GetContributionByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(ContributionToReturn);

        public Task<Result<Contribution>> CreateContributionAsync(CreateContributionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateContributionAsync(UpdateContributionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteContributionAsync(int contributionId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);
    }

    private sealed class RecordingAccountService : IAccountService
    {
        public Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Account>>([new Account(1, "Test Account", new AccountType(1, "Checking", new AccountTypeGroup(1, "Assets")), 1)]);

        public Task<Account?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Account?>(null);

        public Task<Result<Account>> CreateAccountAsync(CreateAccountCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Account>.Failure("Not implemented in test."));

        public Task<Result> UpdateAccountAsync(UpdateAccountCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> ReorderAccountsAsync(ReorderAccountCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }
}