using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Controllers;
using PerFi.API.Requests;
using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.API;

public sealed class AccountsControllerTests
{
    [Fact]
    public async Task GetAll_PreservesDisplayOrderInResponse()
    {
        var accountService = new RecordingAccountService();
        var institutionService = new RecordingInstitutionService();
        var controller = new AccountsController(accountService, institutionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.AccountResponse>>(ok.Value);
        var account = Assert.Single(response);

        Assert.Equal(7, account.DisplayOrder);
    }

    [Fact]
    public async Task Reorder_PassesOrderedIdsToService()
    {
        var accountService = new RecordingAccountService();
        var institutionService = new RecordingInstitutionService();
        var controller = new AccountsController(accountService, institutionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Reorder(new ReorderAccountsRequest([3, 1, 2]));

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(accountService.LastReorderCommand);
        Assert.Equal([3, 1, 2], accountService.LastReorderCommand!.OrderedAccountIds);
    }

    private sealed class RecordingAccountService : IAccountService
    {
        public ReorderAccountCommand? LastReorderCommand { get; private set; }

        public Task<IReadOnlyList<Account>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Account>>([
                new Account(1, "Checking", BuildAccountType(), 99)
                {
                    DisplayOrder = 7
                }]);

        public Task<Account?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Account?>(null);

        public Task<Result<Account>> CreateAccountAsync(CreateAccountCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Account>.Failure("Not implemented in test."));

        public Task<Result> UpdateAccountAsync(UpdateAccountCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> ReorderAccountsAsync(ReorderAccountCommand command, CancellationToken cancellationToken = default)
        {
            LastReorderCommand = command;
            return Task.FromResult(Result.Success());
        }

        private static AccountType BuildAccountType()
            => new AccountType(1, "Checking", new AccountTypeGroup(1, "Assets"))
            {
                DisplayOrder = 2
            };
    }

    private sealed class RecordingInstitutionService : IInstitutionService
    {
        public Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Institution>>([
                new Institution(99, "First Bank", [ ])
                {
                    DisplayOrder = 3
                }]);

        public Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Institution?>(null);

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