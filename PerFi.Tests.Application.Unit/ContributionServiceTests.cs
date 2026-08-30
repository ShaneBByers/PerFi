using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class ContributionServiceTests
{
    private static ContributionContributor CreateContributor(int id = 1) => new(id, "Alice");

    private static Account CreateAccount(int id = 1)
        => new(id, "Checking", new AccountType(1, "Checking", new AccountTypeGroup(1, "Assets")), 1);

    [Fact]
    public async Task CreateContributionAsync_WithMissingContributor_ReturnsFailure()
    {
        var contributorRepo = new Mock<IContributionContributorRepository>();
        contributorRepo.Setup(r => r.GetContributionContributorByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((ContributionContributor?)null);

        var service = new ContributionService(Mock.Of<IContributionRepository>(), contributorRepo.Object, Mock.Of<IAccountRepository>());

        var result = await service.CreateContributionAsync(new CreateContributionCommand(new DateOnly(2026, 8, 9), 25m, 1, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateContributionAsync_WithMissingAccount_ReturnsFailure()
    {
        var contributorRepo = new Mock<IContributionContributorRepository>();
        contributorRepo.Setup(r => r.GetContributionContributorByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateContributor());

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetAccountByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        var service = new ContributionService(Mock.Of<IContributionRepository>(), contributorRepo.Object, accountRepo.Object);

        var result = await service.CreateContributionAsync(new CreateContributionCommand(new DateOnly(2026, 8, 9), 25m, 1, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateContributionAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var contributorRepo = new Mock<IContributionContributorRepository>();
        contributorRepo.Setup(r => r.GetContributionContributorByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateContributor());

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetAccountByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(CreateAccount());

        var contributionRepo = new Mock<IContributionRepository>();
        contributionRepo.Setup(r => r.AddContributionAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(6));

        var service = new ContributionService(contributionRepo.Object, contributorRepo.Object, accountRepo.Object);

        var result = await service.CreateContributionAsync(new CreateContributionCommand(new DateOnly(2026, 8, 9), 25m, 1, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateContributionAsync_WithMissingContributor_ReturnsFailure()
    {
        var contributorRepo = new Mock<IContributionContributorRepository>();
        contributorRepo.Setup(r => r.GetContributionContributorByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((ContributionContributor?)null);

        var service = new ContributionService(Mock.Of<IContributionRepository>(), contributorRepo.Object, Mock.Of<IAccountRepository>());

        var result = await service.UpdateContributionAsync(new UpdateContributionCommand(1, new DateOnly(2026, 8, 9), 25m, 1, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteContributionAsync_DelegatesToRepository()
    {
        var repo = new Mock<IContributionRepository>();
        repo.Setup(r => r.DeleteContributionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new ContributionService(repo.Object, Mock.Of<IContributionContributorRepository>(), Mock.Of<IAccountRepository>());

        var result = await service.DeleteContributionAsync(1);

        Assert.True(result.IsSuccess);
    }
}