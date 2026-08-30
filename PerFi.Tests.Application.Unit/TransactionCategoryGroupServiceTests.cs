using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class TransactionCategoryGroupServiceTests
{
    [Fact]
    public async Task CreateTransactionCategoryGroupAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var repo = new Mock<ITransactionCategoryGroupRepository>();
        repo.Setup(r => r.AddTransactionCategoryGroupAsync(It.IsAny<TransactionCategoryGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(6));

        var service = new TransactionCategoryGroupService(repo.Object);

        var result = await service.CreateTransactionCategoryGroupAsync(new CreateTransactionCategoryGroupCommand("Expenses"));

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateTransactionCategoryGroupAsync_WithInvalidName_ReturnsFailure()
    {
        var service = new TransactionCategoryGroupService(Mock.Of<ITransactionCategoryGroupRepository>());

        var result = await service.UpdateTransactionCategoryGroupAsync(new UpdateTransactionCategoryGroupCommand(1, "   "));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReorderTransactionCategoryGroupsAsync_WithValidCommand_PassesOrderedIds()
    {
        var repo = new Mock<ITransactionCategoryGroupRepository>();
        repo.Setup(r => r.ReorderTransactionCategoryGroupsAsync(new List<int> { 7, 8 }, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new TransactionCategoryGroupService(repo.Object);

        var result = await service.ReorderTransactionCategoryGroupsAsync(new ReorderTransactionCategoryGroupsCommand([7, 8]));

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.ReorderTransactionCategoryGroupsAsync(new List<int> { 7, 8 }, It.IsAny<CancellationToken>()), Times.Once);
    }
}