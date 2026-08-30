using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class TransactionCategoryServiceTests
{
    private static TransactionCategoryGroup CreateGroup(int id = 1) => new(id, "Expenses");

    [Fact]
    public async Task CreateTransactionCategoryAsync_WithMissingGroup_ReturnsFailure()
    {
        var groupRepo = new Mock<ITransactionCategoryGroupRepository>();
        groupRepo.Setup(r => r.GetTransactionCategoryGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((TransactionCategoryGroup?)null);

        var service = new TransactionCategoryService(Mock.Of<ITransactionCategoryRepository>(), groupRepo.Object);

        var result = await service.CreateTransactionCategoryAsync(new CreateTransactionCategoryCommand("Groceries", 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateTransactionCategoryAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var groupRepo = new Mock<ITransactionCategoryGroupRepository>();
        groupRepo.Setup(r => r.GetTransactionCategoryGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateGroup());

        var repo = new Mock<ITransactionCategoryRepository>();
        repo.Setup(r => r.AddTransactionCategoryAsync(It.IsAny<TransactionCategory>(), 1, It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(6));

        var service = new TransactionCategoryService(repo.Object, groupRepo.Object);

        var result = await service.CreateTransactionCategoryAsync(new CreateTransactionCategoryCommand("Groceries", 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateTransactionCategoryAsync_WithMissingGroup_ReturnsFailure()
    {
        var groupRepo = new Mock<ITransactionCategoryGroupRepository>();
        groupRepo.Setup(r => r.GetTransactionCategoryGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((TransactionCategoryGroup?)null);

        var service = new TransactionCategoryService(Mock.Of<ITransactionCategoryRepository>(), groupRepo.Object);

        var result = await service.UpdateTransactionCategoryAsync(new UpdateTransactionCategoryCommand(1, "Groceries", 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReorderTransactionCategoriesAsync_WithValidCommand_PassesOrderedIds()
    {
        var repo = new Mock<ITransactionCategoryRepository>();
        repo.Setup(r => r.ReorderTransactionCategoriesAsync(new List<int> { 7, 8 }, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new TransactionCategoryService(repo.Object, Mock.Of<ITransactionCategoryGroupRepository>());

        var result = await service.ReorderTransactionCategoriesAsync(new ReorderTransactionCategoriesCommand([7, 8]));

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.ReorderTransactionCategoriesAsync(new List<int> { 7, 8 }, It.IsAny<CancellationToken>()), Times.Once);
    }
}