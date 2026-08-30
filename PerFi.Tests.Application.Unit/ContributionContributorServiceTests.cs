using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class ContributionContributorServiceTests
{
    [Fact]
    public async Task GetAllContributionContributorsAsync_DelegatesToRepository()
    {
        var repo = new Mock<IContributionContributorRepository>();
        var contributors = new List<ContributionContributor> { new(1, "Alice") };
        repo.Setup(r => r.GetAllContributionContributorsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(contributors);

        var service = new ContributionContributorService(repo.Object);

        var result = await service.GetAllContributionContributorsAsync();

        Assert.Same(contributors, result);
    }

    [Fact]
    public async Task CreateContributionContributorAsync_WithInvalidName_ReturnsFailure()
    {
        var service = new ContributionContributorService(Mock.Of<IContributionContributorRepository>());

        var result = await service.CreateContributionContributorAsync(new CreateContributionContributorCommand("   "));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateContributionContributorAsync_WhenRepositoryFails_ReturnsFailure()
    {
        var repo = new Mock<IContributionContributorRepository>();
        repo.Setup(r => r.AddContributionContributorAsync(It.IsAny<ContributionContributor>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Failure("db error"));

        var service = new ContributionContributorService(repo.Object);

        var result = await service.CreateContributionContributorAsync(new CreateContributionContributorCommand("Alice"));

        Assert.True(result.IsFailure);
        Assert.Equal("db error", result.Error);
    }

    [Fact]
    public async Task CreateContributionContributorAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var repo = new Mock<IContributionContributorRepository>();
        repo.Setup(r => r.AddContributionContributorAsync(It.IsAny<ContributionContributor>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(6));

        var service = new ContributionContributorService(repo.Object);

        var result = await service.CreateContributionContributorAsync(new CreateContributionContributorCommand("Alice"));

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateContributionContributorAsync_WithInvalidName_ReturnsFailure()
    {
        var service = new ContributionContributorService(Mock.Of<IContributionContributorRepository>());

        var result = await service.UpdateContributionContributorAsync(new UpdateContributionContributorCommand(1, "   "));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateContributionContributorAsync_WithValidCommand_ReturnsSuccess()
    {
        var repo = new Mock<IContributionContributorRepository>();
        repo.Setup(r => r.UpdateContributionContributorAsync(It.IsAny<ContributionContributor>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new ContributionContributorService(repo.Object);

        var result = await service.UpdateContributionContributorAsync(new UpdateContributionContributorCommand(1, "Alice"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ReorderContributionContributorsAsync_WithValidCommand_PassesOrderedIds()
    {
        var repo = new Mock<IContributionContributorRepository>();
        repo.Setup(r => r.ReorderContributionContributorsAsync(new List<int> { 7, 8 }, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new ContributionContributorService(repo.Object);

        var result = await service.ReorderContributionContributorsAsync(new ReorderContributionContributorsCommand([7, 8]));

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.ReorderContributionContributorsAsync(new List<int> { 7, 8 }, It.IsAny<CancellationToken>()), Times.Once);
    }
}