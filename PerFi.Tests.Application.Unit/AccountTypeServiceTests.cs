using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class AccountTypeServiceTests
{
    private static AccountTypeGroup CreateGroup(int id = 1) => new(id, "Assets");

    [Fact]
    public async Task GetAllAccountTypesAsync_DelegatesToRepository()
    {
        var repo = new Mock<IAccountTypeRepository>();
        var types = new List<AccountType> { new(1, "Checking", CreateGroup()) };
        repo.Setup(r => r.GetAllAccountTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(types);

        var service = new AccountTypeService(repo.Object, Mock.Of<IAccountTypeGroupRepository>());

        var result = await service.GetAllAccountTypesAsync();

        Assert.Same(types, result);
    }

    [Fact]
    public async Task GetAccountTypeByIdAsync_DelegatesToRepository()
    {
        var repo = new Mock<IAccountTypeRepository>();
        repo.Setup(r => r.GetAccountTypeByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((AccountType?)null);

        var service = new AccountTypeService(repo.Object, Mock.Of<IAccountTypeGroupRepository>());

        var result = await service.GetAccountTypeByIdAsync(9);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAccountTypeAsync_WithMissingGroup_ReturnsFailure()
    {
        var groupRepo = new Mock<IAccountTypeGroupRepository>();
        groupRepo.Setup(r => r.GetAccountTypeGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((AccountTypeGroup?)null);

        var service = new AccountTypeService(Mock.Of<IAccountTypeRepository>(), groupRepo.Object);

        var result = await service.CreateAccountTypeAsync(new CreateAccountTypeCommand("Checking", 1));

        Assert.True(result.IsFailure);
        Assert.Contains("Account type group with ID", result.Error);
    }

    [Fact]
    public async Task CreateAccountTypeAsync_WithInvalidName_ReturnsFailureFromArgumentException()
    {
        var groupRepo = new Mock<IAccountTypeGroupRepository>();
        groupRepo.Setup(r => r.GetAccountTypeGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateGroup());

        var service = new AccountTypeService(Mock.Of<IAccountTypeRepository>(), groupRepo.Object);

        var result = await service.CreateAccountTypeAsync(new CreateAccountTypeCommand("   ", 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateAccountTypeAsync_WhenRepositoryFails_ReturnsFailure()
    {
        var groupRepo = new Mock<IAccountTypeGroupRepository>();
        groupRepo.Setup(r => r.GetAccountTypeGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateGroup());

        var typeRepo = new Mock<IAccountTypeRepository>();
        typeRepo.Setup(r => r.AddAccountTypeAsync(It.IsAny<AccountType>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure("db error"));

        var service = new AccountTypeService(typeRepo.Object, groupRepo.Object);

        var result = await service.CreateAccountTypeAsync(new CreateAccountTypeCommand("Checking", 1));

        Assert.True(result.IsFailure);
        Assert.Equal("db error", result.Error);
    }

    [Fact]
    public async Task CreateAccountTypeAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var groupRepo = new Mock<IAccountTypeGroupRepository>();
        groupRepo.Setup(r => r.GetAccountTypeGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateGroup());

        var typeRepo = new Mock<IAccountTypeRepository>();
        typeRepo.Setup(r => r.AddAccountTypeAsync(It.IsAny<AccountType>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(11));

        var service = new AccountTypeService(typeRepo.Object, groupRepo.Object);

        var result = await service.CreateAccountTypeAsync(new CreateAccountTypeCommand("Checking", 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(11, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateAccountTypeAsync_WithMissingGroup_ReturnsFailure()
    {
        var groupRepo = new Mock<IAccountTypeGroupRepository>();
        groupRepo.Setup(r => r.GetAccountTypeGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((AccountTypeGroup?)null);

        var service = new AccountTypeService(Mock.Of<IAccountTypeRepository>(), groupRepo.Object);

        var result = await service.UpdateAccountTypeAsync(new UpdateAccountTypeCommand(1, "Checking", 1));

        Assert.True(result.IsFailure);
        Assert.Contains("Account type group with ID", result.Error);
    }

    [Fact]
    public async Task UpdateAccountTypeAsync_WithInvalidName_ReturnsFailureFromArgumentException()
    {
        var groupRepo = new Mock<IAccountTypeGroupRepository>();
        groupRepo.Setup(r => r.GetAccountTypeGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateGroup());

        var service = new AccountTypeService(Mock.Of<IAccountTypeRepository>(), groupRepo.Object);

        var result = await service.UpdateAccountTypeAsync(new UpdateAccountTypeCommand(1, "   ", 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateAccountTypeAsync_WithValidCommand_ReturnsSuccess()
    {
        var groupRepo = new Mock<IAccountTypeGroupRepository>();
        groupRepo.Setup(r => r.GetAccountTypeGroupByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateGroup());

        var typeRepo = new Mock<IAccountTypeRepository>();
        typeRepo.Setup(r => r.UpdateAccountTypeAsync(It.IsAny<AccountType>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new AccountTypeService(typeRepo.Object, groupRepo.Object);

        var result = await service.UpdateAccountTypeAsync(new UpdateAccountTypeCommand(1, "Checking", 1));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAccountTypeAsync_DelegatesToRepository()
    {
        var typeRepo = new Mock<IAccountTypeRepository>();
        typeRepo.Setup(r => r.DeleteAccountTypeAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new AccountTypeService(typeRepo.Object, Mock.Of<IAccountTypeGroupRepository>());

        var result = await service.DeleteAccountTypeAsync(1);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ReorderAccountTypesAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new AccountTypeService(Mock.Of<IAccountTypeRepository>(), Mock.Of<IAccountTypeGroupRepository>());

        var result = await service.ReorderAccountTypesAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReorderAccountTypesAsync_WithValidCommand_PassesOrderedIds()
    {
        var typeRepo = new Mock<IAccountTypeRepository>();
        typeRepo.Setup(r => r.ReorderAccountTypesAsync(new List<int> { 5, 4, 3 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new AccountTypeService(typeRepo.Object, Mock.Of<IAccountTypeGroupRepository>());

        var result = await service.ReorderAccountTypesAsync(new ReorderAccountTypeCommand([5, 4, 3]));

        Assert.True(result.IsSuccess);
        typeRepo.Verify(r => r.ReorderAccountTypesAsync(new List<int> { 5, 4, 3 }, It.IsAny<CancellationToken>()), Times.Once);
    }
}
