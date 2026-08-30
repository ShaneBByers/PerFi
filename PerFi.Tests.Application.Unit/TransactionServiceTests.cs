using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class TransactionServiceTests
{
    private static TransactionCategoryGroup CreateGroup(int id = 1) => new(id, "Expenses");
    private static TransactionCategory CreateCategory(int id = 1) => new(id, "Groceries", CreateGroup());
    private static Account CreateAccount(int id = 1) => new(id, "Checking", new AccountType(1, "Checking", new AccountTypeGroup(1, "Assets")), 1);

    [Fact]
    public async Task CreateTransactionAsync_WithMissingCategory_ReturnsFailure()
    {
        var categoryRepo = new Mock<ITransactionCategoryRepository>();
        categoryRepo.Setup(r => r.GetTransactionCategoryByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((TransactionCategory?)null);

        var service = new TransactionService(Mock.Of<ITransactionRepository>(), categoryRepo.Object, Mock.Of<IAccountRepository>());

        var result = await service.CreateTransactionAsync(new CreateTransactionCommand(new DateOnly(2026, 8, 9), "Store", 25m, 1, 1, null));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithMissingAccount_ReturnsFailure()
    {
        var categoryRepo = new Mock<ITransactionCategoryRepository>();
        categoryRepo.Setup(r => r.GetTransactionCategoryByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateCategory());

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetAccountByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        var service = new TransactionService(Mock.Of<ITransactionRepository>(), categoryRepo.Object, accountRepo.Object);

        var result = await service.CreateTransactionAsync(new CreateTransactionCommand(new DateOnly(2026, 8, 9), "Store", 25m, 1, 1, null));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var categoryRepo = new Mock<ITransactionCategoryRepository>();
        categoryRepo.Setup(r => r.GetTransactionCategoryByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateCategory());

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetAccountByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccount());

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(6));

        var service = new TransactionService(transactionRepo.Object, categoryRepo.Object, accountRepo.Object);

        var result = await service.CreateTransactionAsync(new CreateTransactionCommand(new DateOnly(2026, 8, 9), "Store", 25m, 1, 1, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithMissingCategory_ReturnsFailure()
    {
        var categoryRepo = new Mock<ITransactionCategoryRepository>();
        categoryRepo.Setup(r => r.GetTransactionCategoryByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((TransactionCategory?)null);

        var service = new TransactionService(Mock.Of<ITransactionRepository>(), categoryRepo.Object, Mock.Of<IAccountRepository>());

        var result = await service.UpdateTransactionAsync(new UpdateTransactionCommand(1, new DateOnly(2026, 8, 9), "Store", 25m, 1, 1, null));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteTransactionAsync_DelegatesToRepository()
    {
        var repo = new Mock<ITransactionRepository>();
        repo.Setup(r => r.DeleteTransactionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new TransactionService(repo.Object, Mock.Of<ITransactionCategoryRepository>(), Mock.Of<IAccountRepository>());

        var result = await service.DeleteTransactionAsync(1);

        Assert.True(result.IsSuccess);
    }
}