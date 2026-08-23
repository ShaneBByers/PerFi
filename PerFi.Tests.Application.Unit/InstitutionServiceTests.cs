using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class InstitutionServiceTests
{
    [Fact]
    public async Task GetAllInstitutionsAsync_DelegatesToRepository()
    {
        var repo = new Mock<IInstitutionRepository>();
        var institutions = new List<Institution> { new(1, "First Bank", []) };
        repo.Setup(r => r.GetAllInstitutionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(institutions);

        var service = new InstitutionService(repo.Object);

        var result = await service.GetAllInstitutionsAsync();

        Assert.Same(institutions, result);
    }

    [Fact]
    public async Task GetInstitutionByIdAsync_DelegatesToRepository()
    {
        var repo = new Mock<IInstitutionRepository>();
        repo.Setup(r => r.GetInstitutionByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((Institution?)null);

        var service = new InstitutionService(repo.Object);

        var result = await service.GetInstitutionByIdAsync(2);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateInstitutionAsync_WithInvalidName_ReturnsFailureFromArgumentException()
    {
        var service = new InstitutionService(Mock.Of<IInstitutionRepository>());

        var result = await service.CreateInstitutionAsync(new CreateInstitutionCommand("   "));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateInstitutionAsync_WhenRepositoryFails_ReturnsFailure()
    {
        var repo = new Mock<IInstitutionRepository>();
        repo.Setup(r => r.AddInstitutionAsync(It.IsAny<Institution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure("db error"));

        var service = new InstitutionService(repo.Object);

        var result = await service.CreateInstitutionAsync(new CreateInstitutionCommand("First Bank"));

        Assert.True(result.IsFailure);
        Assert.Equal("db error", result.Error);
    }

    [Fact]
    public async Task CreateInstitutionAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var repo = new Mock<IInstitutionRepository>();
        repo.Setup(r => r.AddInstitutionAsync(It.IsAny<Institution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(14));

        var service = new InstitutionService(repo.Object);

        var result = await service.CreateInstitutionAsync(new CreateInstitutionCommand("First Bank"));

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateInstitutionAsync_WithMissingInstitution_ReturnsFailure()
    {
        var repo = new Mock<IInstitutionRepository>();
        repo.Setup(r => r.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Institution?)null);

        var service = new InstitutionService(repo.Object);

        var result = await service.UpdateInstitutionAsync(new UpdateInstitutionCommand(1, "First Bank"));

        Assert.True(result.IsFailure);
        Assert.Contains("Institution with ID", result.Error);
    }

    [Fact]
    public async Task UpdateInstitutionAsync_WithInvalidName_ReturnsFailureFromArgumentException()
    {
        var repo = new Mock<IInstitutionRepository>();
        repo.Setup(r => r.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Institution(1, "First Bank", []));

        var service = new InstitutionService(repo.Object);

        var result = await service.UpdateInstitutionAsync(new UpdateInstitutionCommand(1, "   "));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateInstitutionAsync_WithValidCommand_ReturnsSuccess()
    {
        var repo = new Mock<IInstitutionRepository>();
        repo.Setup(r => r.GetInstitutionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Institution(1, "First Bank", []));
        repo.Setup(r => r.UpdateInstitutionAsync(It.IsAny<Institution>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new InstitutionService(repo.Object);

        var result = await service.UpdateInstitutionAsync(new UpdateInstitutionCommand(1, "Second Bank"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteInstitutionAsync_DelegatesToRepository()
    {
        var repo = new Mock<IInstitutionRepository>();
        repo.Setup(r => r.DeleteInstitutionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new InstitutionService(repo.Object);

        var result = await service.DeleteInstitutionAsync(1);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ReorderInstitutionsAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new InstitutionService(Mock.Of<IInstitutionRepository>());

        var result = await service.ReorderInstitutionsAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReorderInstitutionsAsync_WithValidCommand_PassesOrderedIds()
    {
        var repo = new Mock<IInstitutionRepository>();
        repo.Setup(r => r.ReorderInstitutionsAsync(new List<int> { 11, 22, 33 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new InstitutionService(repo.Object);

        var result = await service.ReorderInstitutionsAsync(new ReorderInstitutionCommand([11, 22, 33]));

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.ReorderInstitutionsAsync(new List<int> { 11, 22, 33 }, It.IsAny<CancellationToken>()), Times.Once);
    }
}
