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

public sealed class OrderingControllersTests
{
    [Fact]
    public async Task Institutions_GetAll_PreservesDisplayOrderInResponse()
    {
        var institutionService = new RecordingInstitutionService();
        var controller = new InstitutionsController(institutionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.InstitutionResponse>>(ok.Value);
        var institution = Assert.Single(response);

        Assert.Equal(4, institution.DisplayOrder);
        Assert.Single(institution.Accounts);
        Assert.Equal(9, institution.Accounts[0].DisplayOrder);
    }

    [Fact]
    public async Task Institutions_Reorder_PassesOrderedIdsToService()
    {
        var institutionService = new RecordingInstitutionService();
        var controller = new InstitutionsController(institutionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Reorder(new ReorderInstitutionsRequest([11, 22, 33]));

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(institutionService.LastReorderCommand);
        Assert.Equal([11, 22, 33], institutionService.LastReorderCommand!.OrderedInstitutionIds);
    }

    [Fact]
    public async Task AccountTypes_GetAll_PreservesDisplayOrderInResponse()
    {
        var accountTypeService = new RecordingAccountTypeService();
        var controller = new AccountTypesController(accountTypeService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.AccountTypeResponse>>(ok.Value);
        var accountType = Assert.Single(response);

        Assert.Equal(6, accountType.DisplayOrder);
        Assert.Equal("Assets", accountType.Group.Name);
    }

    [Fact]
    public async Task AccountTypes_Reorder_PassesOrderedIdsToService()
    {
        var accountTypeService = new RecordingAccountTypeService();
        var controller = new AccountTypesController(accountTypeService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Reorder(new ReorderAccountTypesRequest([5, 4, 3]));

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(accountTypeService.LastReorderCommand);
        Assert.Equal([5, 4, 3], accountTypeService.LastReorderCommand!.OrderedAccountTypeIds);
    }

    [Fact]
    public async Task AccountTypeGroups_GetAll_PreservesDisplayOrderInResponse()
    {
        var accountTypeGroupService = new RecordingAccountTypeGroupService();
        var controller = new AccountTypeGroupsController(accountTypeGroupService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.AccountTypeGroupResponse>>(ok.Value);
        var group = Assert.Single(response);

        Assert.Equal(12, group.DisplayOrder);
    }

    [Fact]
    public async Task AccountTypeGroups_Reorder_PassesOrderedIdsToService()
    {
        var accountTypeGroupService = new RecordingAccountTypeGroupService();
        var controller = new AccountTypeGroupsController(accountTypeGroupService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Reorder(new ReorderAccountTypeGroupsRequest([7, 8]));

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(accountTypeGroupService.LastReorderCommand);
        Assert.Equal([7, 8], accountTypeGroupService.LastReorderCommand!.OrderedAccountTypeGroupIds);
    }

    private sealed class RecordingInstitutionService : IInstitutionService
    {
        public ReorderInstitutionCommand? LastReorderCommand { get; private set; }

        public Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Institution>>([
                new Institution(1, "Alpha Bank", [new Account(9, "Checking", BuildAccountType(), 1) { DisplayOrder = 9 }])
                {
                    DisplayOrder = 4
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
        {
            LastReorderCommand = command;
            return Task.FromResult(Result.Success());
        }

        private static AccountType BuildAccountType()
            => new AccountType(2, "Checking", new AccountTypeGroup(3, "Assets"))
            {
                DisplayOrder = 8
            };
    }

    private sealed class RecordingAccountTypeService : IAccountTypeService
    {
        public ReorderAccountTypeCommand? LastReorderCommand { get; private set; }

        public Task<IReadOnlyList<AccountType>> GetAllAccountTypesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AccountType>>([
                new AccountType(1, "Checking", new AccountTypeGroup(1, "Assets"))
                {
                    DisplayOrder = 6
                }]);

        public Task<AccountType?> GetAccountTypeByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<AccountType?>(null);

        public Task<Result<AccountType>> CreateAccountTypeAsync(CreateAccountTypeCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<AccountType>.Failure("Not implemented in test."));

        public Task<Result> UpdateAccountTypeAsync(UpdateAccountTypeCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> DeleteAccountTypeAsync(int accountTypeId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> ReorderAccountTypesAsync(ReorderAccountTypeCommand command, CancellationToken cancellationToken = default)
        {
            LastReorderCommand = command;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class RecordingAccountTypeGroupService : IAccountTypeGroupService
    {
        public ReorderAccountTypeGroupCommand? LastReorderCommand { get; private set; }

        public Task<IReadOnlyList<AccountTypeGroup>> GetAllAccountTypeGroupsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AccountTypeGroup>>([
                new AccountTypeGroup(1, "Assets")
                {
                    DisplayOrder = 12
                }]);

        public Task<AccountTypeGroup?> GetAccountTypeGroupByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<AccountTypeGroup?>(null);

        public Task<Result<AccountTypeGroup>> CreateAccountTypeGroupAsync(CreateAccountTypeGroupCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<AccountTypeGroup>.Failure("Not implemented in test."));

        public Task<Result> UpdateAccountTypeGroupAsync(UpdateAccountTypeGroupCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> DeleteAccountTypeGroupAsync(int accountTypeGroupId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> ReorderAccountTypeGroupsAsync(ReorderAccountTypeGroupCommand command, CancellationToken cancellationToken = default)
        {
            LastReorderCommand = command;
            return Task.FromResult(Result.Success());
        }
    }
}
