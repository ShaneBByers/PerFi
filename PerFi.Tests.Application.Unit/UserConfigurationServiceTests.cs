using Moq;
using PerFi.Application.Commands;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class UserConfigurationServiceTests
{
    private static CreateUserConfigurationCommand CreateCommand()
        => new(new DateOnly(1990, 1, 1), PayCycleType.BiWeekly, new DateOnly(2026, 1, 2), 0.03m, 0.025m);

    [Fact]
    public async Task GetUserConfigurationAsync_DelegatesToRepository()
    {
        var config = new UserConfiguration(1, new DateOnly(1990, 1, 1), PayCycleType.BiWeekly, new DateOnly(2026, 1, 2), 0.03m, 0.025m);
        var repo = new Mock<IUserConfigurationRepository>();
        repo.Setup(r => r.GetUserConfigurationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(config);

        var service = new UserConfigurationService(repo.Object);

        var result = await service.GetUserConfigurationAsync();

        Assert.Same(config, result);
    }

    [Fact]
    public async Task CreateUserConfigurationAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new UserConfigurationService(Mock.Of<IUserConfigurationRepository>());

        var result = await service.CreateUserConfigurationAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateUserConfigurationAsync_WithValidCommand_ReturnsSuccessWithAssignedId()
    {
        var repo = new Mock<IUserConfigurationRepository>();
        repo.Setup(r => r.AddUserConfigurationAsync(It.IsAny<UserConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(9));

        var service = new UserConfigurationService(repo.Object);

        var result = await service.CreateUserConfigurationAsync(CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(9, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateUserConfigurationAsync_WithNullCommand_ReturnsFailure()
    {
        var service = new UserConfigurationService(Mock.Of<IUserConfigurationRepository>());

        var result = await service.UpdateUserConfigurationAsync(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateUserConfigurationAsync_DelegatesToRepository()
    {
        var repo = new Mock<IUserConfigurationRepository>();
        repo.Setup(r => r.UpdateUserConfigurationAsync(It.IsAny<UserConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new UserConfigurationService(repo.Object);

        var result = await service.UpdateUserConfigurationAsync(new UpdateUserConfigurationCommand(1, new DateOnly(1990, 1, 1), PayCycleType.BiWeekly, new DateOnly(2026, 1, 2), 0.03m, 0.025m));

        Assert.True(result.IsSuccess);
    }
}
