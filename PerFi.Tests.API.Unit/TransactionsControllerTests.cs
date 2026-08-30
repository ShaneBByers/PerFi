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

public sealed class TransactionsControllerTests
{
    private static TransactionsController CreateController(RecordingTransactionService transactionService, RecordingAccountService? accountService = null)
        => new(transactionService, accountService ?? new RecordingAccountService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task GetAll_UsesAccountNamesInResponse()
    {
        var controller = CreateController(new RecordingTransactionService(), new RecordingAccountService());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerFi.API.Responses.TransactionResponse>>(ok.Value);
        var transaction = Assert.Single(response);

        Assert.Equal("Test Account", transaction.Account.Name);
        Assert.Equal("Groceries", transaction.Category.Name);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var controller = CreateController(new RecordingTransactionService { TransactionToReturn = null });

        var result = await controller.Get(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenFound_ReturnsOk()
    {
        var controller = CreateController(new RecordingTransactionService { TransactionToReturn = BuildTransaction() });

        var result = await controller.Get(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerFi.API.Responses.TransactionResponse>(ok.Value);
        Assert.Equal(25m, response.Amount);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingTransactionService());

        var result = await controller.Create(new CreateTransactionRequest(default, "", 0m, 1, 1, null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new RecordingTransactionService { CreateResult = Result<Transaction>.Failure("nope") };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateTransactionRequest(new DateOnly(2026, 8, 9), "Store", 25m, 1, 1, null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var service = new RecordingTransactionService { CreateResult = Result<Transaction>.Success(BuildTransaction(5)) };
        var controller = CreateController(service);

        var result = await controller.Create(new CreateTransactionRequest(new DateOnly(2026, 8, 9), "Store", 25m, 1, 1, null));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TransactionsController.Get), created.ActionName);
    }

    [Fact]
    public async Task Update_WithInvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController(new RecordingTransactionService());

        var result = await controller.Update(1, new UpdateTransactionRequest(default, "", 0m, 1, 1, null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingTransactionService { UpdateResult = Result.Failure("Transaction with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateTransactionRequest(new DateOnly(2026, 8, 9), "Store", 25m, 1, 1, null));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingTransactionService { UpdateResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Update(1, new UpdateTransactionRequest(new DateOnly(2026, 8, 9), "Store", 25m, 1, 1, null));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new RecordingTransactionService { DeleteResult = Result.Failure("Transaction with ID '1' not found.") };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new RecordingTransactionService { DeleteResult = Result.Success() };
        var controller = CreateController(service);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    private static Transaction BuildTransaction(int id = 1)
        => new(id, new DateOnly(2026, 8, 9), "Store", 25m, new TransactionCategory(1, "Groceries", new TransactionCategoryGroup(1, "Expenses")) { DisplayOrder = 7 }, 1);

    private sealed class RecordingTransactionService : ITransactionService
    {
        public Transaction? TransactionToReturn { get; set; } = BuildTransaction();
        public Result<Transaction> CreateResult { get; set; } = Result<Transaction>.Failure("Not implemented in test.");
        public Result UpdateResult { get; set; } = Result.Failure("Not implemented in test.");
        public Result DeleteResult { get; set; } = Result.Failure("Not implemented in test.");

        public Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Transaction>>([BuildTransaction()]);

        public Task<Transaction?> GetTransactionByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(TransactionToReturn);

        public Task<Result<Transaction>> CreateTransactionAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateTransactionAsync(UpdateTransactionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteTransactionAsync(int transactionId, CancellationToken cancellationToken = default)
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