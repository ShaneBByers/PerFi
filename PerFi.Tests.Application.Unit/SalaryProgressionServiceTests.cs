using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class SalaryProgressionServiceTests
{
    [Fact]
    public async Task GetAllSalaryProgressionsAsync_DelegatesToRepository()
    {
        var entries = new List<SalaryProgression> { new(1, new DateOnly(2026, 1, 1), 100_000m) };
        var repo = new Mock<ISalaryProgressionRepository>();
        repo.Setup(r => r.GetAllSalaryProgressionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entries);

        var service = new SalaryProgressionService(repo.Object);

        var result = await service.GetAllSalaryProgressionsAsync();

        Assert.Same(entries, result);
    }

    [Fact]
    public async Task CreateSalaryProgressionAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new SalaryProgressionService(Mock.Of<ISalaryProgressionRepository>());

        var result = await service.CreateSalaryProgressionAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateSalaryProgressionAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var repo = new Mock<ISalaryProgressionRepository>();
        repo.Setup(r => r.AddSalaryProgressionAsync(It.IsAny<SalaryProgression>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(7));

        var service = new SalaryProgressionService(repo.Object);

        var result = await service.CreateSalaryProgressionAsync(new CreateSalaryProgressionCommand(new DateOnly(2026, 1, 1), 100_000m));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value!.Id);
    }

    [Fact]
    public async Task CreateSalaryProgressionAsync_WithInvalidDate_ReturnsFailureFromArgumentException()
    {
        var service = new SalaryProgressionService(Mock.Of<ISalaryProgressionRepository>());

        var result = await service.CreateSalaryProgressionAsync(new CreateSalaryProgressionCommand(default, 100_000m));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateSalaryProgressionAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new SalaryProgressionService(Mock.Of<ISalaryProgressionRepository>());

        var result = await service.UpdateSalaryProgressionAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteSalaryProgressionAsync_DelegatesToRepository()
    {
        var repo = new Mock<ISalaryProgressionRepository>();
        repo.Setup(r => r.DeleteSalaryProgressionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var service = new SalaryProgressionService(repo.Object);

        var result = await service.DeleteSalaryProgressionAsync(1);

        Assert.True(result.IsSuccess);
    }
}
