using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class AccountContributionPlanServiceTests
{
    private static CreateAccountContributionPlanCommand CreateCommand(int accountId = 1)
        => new(accountId, ContributionContributorType.Self, new DateOnly(2024, 1, 1), 100m, 10m, 0m, 0m, 0m, 0m, 0m, 0m);

    [Fact]
    public async Task GetAllByAccountIdAsync_DelegatesToRepository()
    {
        var plans = new List<AccountContributionPlan> { new(1, 1, ContributionContributorType.Self, new DateOnly(2024, 1, 1), 100m, 0, 0, 0, 0, 0, 0, 0) };
        var repo = new Mock<IAccountContributionPlanRepository>();
        repo.Setup(r => r.GetAllByAccountIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(plans);

        var service = new AccountContributionPlanService(repo.Object);

        var result = await service.GetAllByAccountIdAsync(1);

        Assert.Same(plans, result);
    }

    [Fact]
    public async Task CreateAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new AccountContributionPlanService(Mock.Of<IAccountContributionPlanRepository>());

        var result = await service.CreateAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var repo = new Mock<IAccountContributionPlanRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<AccountContributionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(3));

        var service = new AccountContributionPlanService(repo.Object);

        var result = await service.CreateAsync(CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Id);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidAccountId_ReturnsFailureFromArgumentException()
    {
        var service = new AccountContributionPlanService(Mock.Of<IAccountContributionPlanRepository>());

        var result = await service.CreateAsync(CreateCommand(accountId: 0));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new AccountContributionPlanService(Mock.Of<IAccountContributionPlanRepository>());

        var result = await service.UpdateAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        var repo = new Mock<IAccountContributionPlanRepository>();
        repo.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new AccountContributionPlanService(repo.Object);

        var result = await service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
    }
}
