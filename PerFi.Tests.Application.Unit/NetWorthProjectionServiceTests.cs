using Moq;
using PerFi.Application.Services;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using Xunit;

namespace PerFi.Tests.Application.Unit;

public class NetWorthProjectionServiceTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static (Mock<IAccountRepository> Accounts, Mock<IFinanceSnapshotRepository> Snapshots, Mock<IContributionRepository> Contributions,
        Mock<IAccountContributionPlanRepository> Plans, Mock<ISalaryProgressionRepository> SalaryProgressions, Mock<IUserConfigurationRepository> UserConfiguration)
        CreateMocks()
        => (new Mock<IAccountRepository>(), new Mock<IFinanceSnapshotRepository>(), new Mock<IContributionRepository>(),
            new Mock<IAccountContributionPlanRepository>(), new Mock<ISalaryProgressionRepository>(), new Mock<IUserConfigurationRepository>());

    private static NetWorthProjectionService CreateService(
        Mock<IAccountRepository> accounts, Mock<IFinanceSnapshotRepository> snapshots, Mock<IContributionRepository> contributions,
        Mock<IAccountContributionPlanRepository> plans, Mock<ISalaryProgressionRepository> salaryProgressions, Mock<IUserConfigurationRepository> userConfiguration,
        TimeProvider? timeProvider = null)
        => new(accounts.Object, snapshots.Object, contributions.Object, plans.Object, salaryProgressions.Object, userConfiguration.Object,
            timeProvider ?? new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task GetProjectionAsync_WithNoUserConfiguration_ReturnsEmpty()
    {
        var (accounts, snapshots, contributions, plans, salaryProgressions, userConfiguration) = CreateMocks();
        userConfiguration.Setup(r => r.GetUserConfigurationAsync(It.IsAny<CancellationToken>())).ReturnsAsync((UserConfiguration?)null);

        var service = CreateService(accounts, snapshots, contributions, plans, salaryProgressions, userConfiguration);

        var result = await service.GetProjectionAsync();

        Assert.Empty(result);
        accounts.Verify(r => r.GetAllAccountsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProjectionAsync_WithNoAccounts_ReturnsEmpty()
    {
        var (accounts, snapshots, contributions, plans, salaryProgressions, userConfiguration) = CreateMocks();
        userConfiguration.Setup(r => r.GetUserConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserConfiguration(new DateOnly(1990, 1, 1), PayCycleType.Monthly, new DateOnly(2026, 1, 1), 3m, 2m, 65));
        accounts.Setup(r => r.GetAllAccountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Account>)[]);

        var service = CreateService(accounts, snapshots, contributions, plans, salaryProgressions, userConfiguration);

        var result = await service.GetProjectionAsync();

        Assert.Empty(result);
        snapshots.Verify(r => r.GetAllSnapshotsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProjectionAsync_WithAccountsAndData_ReturnsNonEmptyProjection()
    {
        var (accountsMock, snapshotsMock, contributionsMock, plansMock, salaryProgressionsMock, userConfigurationMock) = CreateMocks();

        var group = new AccountTypeGroup(1, "Assets");
        var type = new AccountType(1, "Checking", group);
        var account = new Account(1, "Test Account", type, 1) { ExpectedAnnualGrowthPercentage = 5m };

        userConfigurationMock.Setup(r => r.GetUserConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserConfiguration(new DateOnly(1990, 1, 1), PayCycleType.Monthly, new DateOnly(2026, 1, 1), 3m, 2m, 65));
        accountsMock.Setup(r => r.GetAllAccountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Account>)[account]);
        snapshotsMock.Setup(r => r.GetAllSnapshotsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<FinanceSnapshot>)[new FinanceSnapshot(new DateOnly(2026, 1, 1), [new AccountBalance(account, 1000m)])]);
        contributionsMock.Setup(r => r.GetAllContributionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Contribution>)[]);
        plansMock.Setup(r => r.GetAllForCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<AccountContributionPlan>)[]);
        salaryProgressionsMock.Setup(r => r.GetAllSalaryProgressionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<SalaryProgression>)[]);

        var service = CreateService(accountsMock, snapshotsMock, contributionsMock, plansMock, salaryProgressionsMock, userConfigurationMock);

        var result = await service.GetProjectionAsync();

        Assert.NotEmpty(result);
    }
}
