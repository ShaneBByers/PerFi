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

public sealed class AccountsControllerTests
{
    private static AccountsController CreateController(RecordingAccountService accountService, RecordingInstitutionService institutionService)
        => new(accountService, institutionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

    [Fact]
    public async Task GetAll_PreservesDisplayOrderInResponse()
    {
        var controller = CreateController(new RecordingAccountService(), new RecordingInstitutionService());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.AccountResponse>>(ok.Value);
        var account = Assert.Single(response);

        Assert.Equal(7, account.DisplayOrder);
    }

    [Fact]
    public async Task Get_WhenAccountMissing_ReturnsNotFound()
    {
        var accountService = new RecordingAccountService { AccountToReturn = null };
        var controller = CreateController(accountService, new RecordingInstitutionService());

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenInstitutionMissing_ReturnsNotFound()
    {
        var accountService = new RecordingAccountService { AccountToReturn = new Account(1, "Checking", BuildAccountType(), 99) };
        var institutionService = new RecordingInstitutionService { InstitutionByIdToReturn = null };
        var controller = CreateController(accountService, institutionService);

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var accountService = new RecordingAccountService { AccountToReturn = new Account(1, "Checking", BuildAccountType(), 99) };
        var institutionService = new RecordingInstitutionService { InstitutionByIdToReturn = new Institution(99, "First Bank", []) };
        var controller = CreateController(accountService, institutionService);

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerFi.API.Responses.AccountResponse>(ok.Value);
        Assert.Equal("Checking", response.Name);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingAccountService(), new RecordingInstitutionService());

        var result = await controller.Create(new CreateAccountRequest("   ", 1, 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var accountService = new RecordingAccountService { CreateResult = Result<Account>.Failure("nope") };
        var controller = CreateController(accountService, new RecordingInstitutionService());

        var result = await controller.Create(new CreateAccountRequest("Checking", 1, 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var accountService = new RecordingAccountService { CreateResult = Result<Account>.Success(new Account(5, "Checking", BuildAccountType(), 1)) };
        var controller = CreateController(accountService, new RecordingInstitutionService());

        var result = await controller.Create(new CreateAccountRequest("Checking", 1, 1));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AccountsController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingAccountService(), new RecordingInstitutionService());

        var result = await controller.Update(1, new UpdateAccountRequest("   ", 1, 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenAccountNotFound_ReturnsNotFound()
    {
        var accountService = new RecordingAccountService { UpdateResult = Result.Failure("Account with ID '1' not found.") };
        var controller = CreateController(accountService, new RecordingInstitutionService());

        var result = await controller.Update(1, new UpdateAccountRequest("Checking", 1, 1));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenOtherFailure_ReturnsBadRequest()
    {
        var accountService = new RecordingAccountService { UpdateResult = Result.Failure("Account name is required.") };
        var controller = CreateController(accountService, new RecordingInstitutionService());

        var result = await controller.Update(1, new UpdateAccountRequest("Checking", 1, 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var accountService = new RecordingAccountService { UpdateResult = Result.Success() };
        var controller = CreateController(accountService, new RecordingInstitutionService());

        var result = await controller.Update(1, new UpdateAccountRequest("Checking", 1, 1));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var accountService = new RecordingAccountService { DeleteResult = Result.Failure("Account with ID '1' not found.") };
        var controller = CreateController(accountService, new RecordingInstitutionService());

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var accountService = new RecordingAccountService { DeleteResult = Result.Success() };
        var controller = CreateController(accountService, new RecordingInstitutionService());

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Reorder_PassesOrderedIdsToService()
    {
        var accountService = new RecordingAccountService();
        var controller = CreateController(accountService, new RecordingInstitutionService());

        var result = await controller.Reorder(new ReorderAccountsRequest([3, 1, 2]));

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(accountService.LastReorderCommand);
        Assert.Equal([3, 1, 2], accountService.LastReorderCommand!.OrderedAccountIds);
    }

    private static AccountType BuildAccountType()
        => new AccountType(1, "Checking", new AccountTypeGroup(1, "Assets"))
        {
            DisplayOrder = 2
        };

    private sealed class RecordingAccountService : IAccountService
    {
        public ReorderAccountCommand? LastReorderCommand { get; private set; }
        public Account? AccountToReturn { get; set; }
        public Result<Account> CreateResult { get; set; } = Result<Account>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Account>>([
                new Account(1, "Checking", BuildAccountType(), 99)
                {
                    DisplayOrder = 7
                }]);

        public Task<Account?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(AccountToReturn);

        public Task<Result<Account>> CreateAccountAsync(CreateAccountCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateAccountAsync(UpdateAccountCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteResult);

        public Task<Result> ReorderAccountsAsync(ReorderAccountCommand command, CancellationToken cancellationToken = default)
        {
            LastReorderCommand = command;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class RecordingInstitutionService : IInstitutionService
    {
        public Institution? InstitutionByIdToReturn { get; set; } = new Institution(99, "First Bank", []);

        public Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Institution>>([
                new Institution(99, "First Bank", [ ])
                {
                    DisplayOrder = 3
                }]);

        public Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(InstitutionByIdToReturn);

        public Task<Result<Institution>> CreateInstitutionAsync(CreateInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Institution>.Failure("Not implemented in test."));

        public Task<Result> UpdateInstitutionAsync(UpdateInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> DeleteInstitutionAsync(int institutionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> ReorderInstitutionsAsync(ReorderInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }
}
