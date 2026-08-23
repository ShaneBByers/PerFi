using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class AccountTypeGroupServiceTests
{
    [Fact]
    public async Task GetAllAccountTypeGroupsAsync_DelegatesToRepository()
    {
        var repo = new Mock<IAccountTypeGroupRepository>();
        var groups = new List<AccountTypeGroup> { new(1, "Assets") };
        repo.Setup(r => r.GetAllAccountTypeGroupsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(groups);

        var service = new AccountTypeGroupService(repo.Object);

        var result = await service.GetAllAccountTypeGroupsAsync();

        Assert.Same(groups, result);
    }

    [Fact]
    public async Task GetAccountTypeGroupByIdAsync_DelegatesToRepository()
    {
        var repo = new Mock<IAccountTypeGroupRepository>();
        repo.Setup(r => r.GetAccountTypeGroupByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync((AccountTypeGroup?)null);

        var service = new AccountTypeGroupService(repo.Object);

        var result = await service.GetAccountTypeGroupByIdAsync(3);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAccountTypeGroupAsync_WithInvalidName_ReturnsFailureFromArgumentException()
    {
        var service = new AccountTypeGroupService(Mock.Of<IAccountTypeGroupRepository>());

        var result = await service.CreateAccountTypeGroupAsync(new CreateAccountTypeGroupCommand("   "));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateAccountTypeGroupAsync_WhenRepositoryFails_ReturnsFailure()
    {
        var repo = new Mock<IAccountTypeGroupRepository>();
        repo.Setup(r => r.AddAccountTypeGroupAsync(It.IsAny<AccountTypeGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure("db error"));

        var service = new AccountTypeGroupService(repo.Object);

        var result = await service.CreateAccountTypeGroupAsync(new CreateAccountTypeGroupCommand("Assets"));

        Assert.True(result.IsFailure);
        Assert.Equal("db error", result.Error);
    }

    [Fact]
    public async Task CreateAccountTypeGroupAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var repo = new Mock<IAccountTypeGroupRepository>();
        repo.Setup(r => r.AddAccountTypeGroupAsync(It.IsAny<AccountTypeGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(6));

        var service = new AccountTypeGroupService(repo.Object);

        var result = await service.CreateAccountTypeGroupAsync(new CreateAccountTypeGroupCommand("Assets"));

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateAccountTypeGroupAsync_WithInvalidName_ReturnsFailureFromArgumentException()
    {
        var service = new AccountTypeGroupService(Mock.Of<IAccountTypeGroupRepository>());

        var result = await service.UpdateAccountTypeGroupAsync(new UpdateAccountTypeGroupCommand(1, "   "));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateAccountTypeGroupAsync_WithValidCommand_ReturnsSuccess()
    {
        var repo = new Mock<IAccountTypeGroupRepository>();
        repo.Setup(r => r.UpdateAccountTypeGroupAsync(It.IsAny<AccountTypeGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new AccountTypeGroupService(repo.Object);

        var result = await service.UpdateAccountTypeGroupAsync(new UpdateAccountTypeGroupCommand(1, "Assets"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAccountTypeGroupAsync_DelegatesToRepository()
    {
        var repo = new Mock<IAccountTypeGroupRepository>();
        repo.Setup(r => r.DeleteAccountTypeGroupAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new AccountTypeGroupService(repo.Object);

        var result = await service.DeleteAccountTypeGroupAsync(1);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ReorderAccountTypeGroupsAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new AccountTypeGroupService(Mock.Of<IAccountTypeGroupRepository>());

        var result = await service.ReorderAccountTypeGroupsAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReorderAccountTypeGroupsAsync_WithValidCommand_PassesOrderedIds()
    {
        var repo = new Mock<IAccountTypeGroupRepository>();
        repo.Setup(r => r.ReorderAccountTypeGroupsAsync(new List<int> { 7, 8 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new AccountTypeGroupService(repo.Object);

        var result = await service.ReorderAccountTypeGroupsAsync(new ReorderAccountTypeGroupCommand([7, 8]));

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.ReorderAccountTypeGroupsAsync(new List<int> { 7, 8 }, It.IsAny<CancellationToken>()), Times.Once);
    }
}
