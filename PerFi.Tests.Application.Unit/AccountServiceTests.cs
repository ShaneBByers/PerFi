using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class AccountServiceTests
{
    private static AccountType CreateAccountType(int id = 1)
        => new(id, "Checking", new AccountTypeGroup(1, "Assets"));

    private static Institution CreateInstitution(int id = 1)
        => new(id, "First Bank", []);

    [Fact]
    public async Task GetAllAccountsAsync_DelegatesToRepository()
    {
        var accountRepo = new Mock<IAccountRepository>();
        var accounts = new List<Account> { new(1, "Checking", CreateAccountType(), 1) };
        accountRepo.Setup(repo => repo.GetAllAccountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(accounts);

        var service = new AccountService(accountRepo.Object, Mock.Of<IAccountTypeRepository>(), Mock.Of<IInstitutionRepository>());

        var result = await service.GetAllAccountsAsync();

        Assert.Same(accounts, result);
    }

    [Fact]
    public async Task GetAccountByIdAsync_DelegatesToRepository()
    {
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.GetAccountByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        var service = new AccountService(accountRepo.Object, Mock.Of<IAccountTypeRepository>(), Mock.Of<IInstitutionRepository>());

        var result = await service.GetAccountByIdAsync(5);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAccountAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new AccountService(Mock.Of<IAccountRepository>(), Mock.Of<IAccountTypeRepository>(), Mock.Of<IInstitutionRepository>());

        var result = await service.CreateAccountAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal("Create account command cannot be null.", result.Error);
    }

    [Fact]
    public async Task CreateAccountAsync_WithMissingAccountType_ReturnsFailure()
    {
        var accountTypeRepo = new Mock<IAccountTypeRepository>();
        accountTypeRepo.Setup(repo => repo.GetAccountTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((AccountType?)null);

        var service = new AccountService(Mock.Of<IAccountRepository>(), accountTypeRepo.Object, Mock.Of<IInstitutionRepository>());

        var result = await service.CreateAccountAsync(new CreateAccountCommand("Checking", 1, 1));

        Assert.True(result.IsFailure);
        Assert.Contains("Account type with ID", result.Error);
    }

    [Fact]
    public async Task CreateAccountAsync_WithMissingInstitution_ReturnsFailure()
    {
        var accountTypeRepo = new Mock<IAccountTypeRepository>();
        accountTypeRepo.Setup(repo => repo.GetAccountTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccountType());

        var institutionRepo = new Mock<IInstitutionRepository>();
        institutionRepo.Setup(repo => repo.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Institution?)null);

        var service = new AccountService(Mock.Of<IAccountRepository>(), accountTypeRepo.Object, institutionRepo.Object);

        var result = await service.CreateAccountAsync(new CreateAccountCommand("Checking", 1, 1));

        Assert.True(result.IsFailure);
        Assert.Contains("Institution with ID", result.Error);
    }

    [Fact]
    public async Task CreateAccountAsync_WithInvalidName_ReturnsFailureFromArgumentException()
    {
        var accountTypeRepo = new Mock<IAccountTypeRepository>();
        accountTypeRepo.Setup(repo => repo.GetAccountTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccountType());

        var institutionRepo = new Mock<IInstitutionRepository>();
        institutionRepo.Setup(repo => repo.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateInstitution());

        var service = new AccountService(Mock.Of<IAccountRepository>(), accountTypeRepo.Object, institutionRepo.Object);

        var result = await service.CreateAccountAsync(new CreateAccountCommand("   ", 1, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenRepositoryFails_ReturnsFailure()
    {
        var accountTypeRepo = new Mock<IAccountTypeRepository>();
        accountTypeRepo.Setup(repo => repo.GetAccountTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccountType());

        var institutionRepo = new Mock<IInstitutionRepository>();
        institutionRepo.Setup(repo => repo.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateInstitution());

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.AddAccountAsync(It.IsAny<Account>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure("db error"));

        var service = new AccountService(accountRepo.Object, accountTypeRepo.Object, institutionRepo.Object);

        var result = await service.CreateAccountAsync(new CreateAccountCommand("Checking", 1, 1));

        Assert.True(result.IsFailure);
        Assert.Equal("db error", result.Error);
    }

    [Fact]
    public async Task CreateAccountAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var accountTypeRepo = new Mock<IAccountTypeRepository>();
        accountTypeRepo.Setup(repo => repo.GetAccountTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccountType());

        var institutionRepo = new Mock<IInstitutionRepository>();
        institutionRepo.Setup(repo => repo.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateInstitution());

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.AddAccountAsync(It.IsAny<Account>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(42));

        var service = new AccountService(accountRepo.Object, accountTypeRepo.Object, institutionRepo.Object);

        var result = await service.CreateAccountAsync(new CreateAccountCommand("Checking", 1, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateAccountAsync_WithMissingAccount_ReturnsFailure()
    {
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.GetAccountByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        var service = new AccountService(accountRepo.Object, Mock.Of<IAccountTypeRepository>(), Mock.Of<IInstitutionRepository>());

        var result = await service.UpdateAccountAsync(new UpdateAccountCommand(1, "Checking", 1, 1));

        Assert.True(result.IsFailure);
        Assert.Contains("Account with ID", result.Error);
    }

    [Fact]
    public async Task UpdateAccountAsync_WithMissingAccountType_ReturnsFailure()
    {
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.GetAccountByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account(1, "Checking", CreateAccountType(), 1));

        var accountTypeRepo = new Mock<IAccountTypeRepository>();
        accountTypeRepo.Setup(repo => repo.GetAccountTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((AccountType?)null);

        var service = new AccountService(accountRepo.Object, accountTypeRepo.Object, Mock.Of<IInstitutionRepository>());

        var result = await service.UpdateAccountAsync(new UpdateAccountCommand(1, "Checking", 1, 1));

        Assert.True(result.IsFailure);
        Assert.Contains("Account type with ID", result.Error);
    }

    [Fact]
    public async Task UpdateAccountAsync_WithMissingInstitution_ReturnsFailure()
    {
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.GetAccountByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account(1, "Checking", CreateAccountType(), 1));

        var accountTypeRepo = new Mock<IAccountTypeRepository>();
        accountTypeRepo.Setup(repo => repo.GetAccountTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccountType());

        var institutionRepo = new Mock<IInstitutionRepository>();
        institutionRepo.Setup(repo => repo.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Institution?)null);

        var service = new AccountService(accountRepo.Object, accountTypeRepo.Object, institutionRepo.Object);

        var result = await service.UpdateAccountAsync(new UpdateAccountCommand(1, "Checking", 1, 1));

        Assert.True(result.IsFailure);
        Assert.Contains("Institution with ID", result.Error);
    }

    [Fact]
    public async Task UpdateAccountAsync_WithInvalidName_ReturnsFailureFromArgumentException()
    {
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.GetAccountByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account(1, "Checking", CreateAccountType(), 1));

        var accountTypeRepo = new Mock<IAccountTypeRepository>();
        accountTypeRepo.Setup(repo => repo.GetAccountTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccountType());

        var institutionRepo = new Mock<IInstitutionRepository>();
        institutionRepo.Setup(repo => repo.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateInstitution());

        var service = new AccountService(accountRepo.Object, accountTypeRepo.Object, institutionRepo.Object);

        var result = await service.UpdateAccountAsync(new UpdateAccountCommand(1, "   ", 1, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateAccountAsync_WithValidCommand_ReturnsSuccess()
    {
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.GetAccountByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account(1, "Checking", CreateAccountType(), 1));
        accountRepo.Setup(repo => repo.UpdateAccountAsync(It.IsAny<Account>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var accountTypeRepo = new Mock<IAccountTypeRepository>();
        accountTypeRepo.Setup(repo => repo.GetAccountTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccountType());

        var institutionRepo = new Mock<IInstitutionRepository>();
        institutionRepo.Setup(repo => repo.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateInstitution());

        var service = new AccountService(accountRepo.Object, accountTypeRepo.Object, institutionRepo.Object);

        var result = await service.UpdateAccountAsync(new UpdateAccountCommand(1, "Checking", 1, 1));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAccountAsync_DelegatesToRepository()
    {
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.DeleteAccountAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new AccountService(accountRepo.Object, Mock.Of<IAccountTypeRepository>(), Mock.Of<IInstitutionRepository>());

        var result = await service.DeleteAccountAsync(1);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ReorderAccountsAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new AccountService(Mock.Of<IAccountRepository>(), Mock.Of<IAccountTypeRepository>(), Mock.Of<IInstitutionRepository>());

        var result = await service.ReorderAccountsAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReorderAccountsAsync_WithValidCommand_PassesOrderedIds()
    {
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(repo => repo.ReorderAccountsAsync(new List<int> { 3, 1, 2 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new AccountService(accountRepo.Object, Mock.Of<IAccountTypeRepository>(), Mock.Of<IInstitutionRepository>());

        var result = await service.ReorderAccountsAsync(new ReorderAccountCommand([3, 1, 2]));

        Assert.True(result.IsSuccess);
        accountRepo.Verify(repo => repo.ReorderAccountsAsync(new List<int> { 3, 1, 2 }, It.IsAny<CancellationToken>()), Times.Once);
    }
}
