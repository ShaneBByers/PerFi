using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Controllers;
using PerFi.API.Requests;
using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.API;

public sealed class SnapshotsControllerTests
{
    [Fact]
    public async Task Create_WithNegativeBalance_PassesBalanceThroughToService()
    {
        var snapshotService = new RecordingFinanceSnapshotService();
        var institutionService = new RecordingInstitutionService();
        var controller = new SnapshotsController(snapshotService, institutionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var request = new CreateFinanceSnapshotRequest(
            new DateOnly(2026, 8, 9),
            new Dictionary<int, decimal> { [42] = -125.50m });

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(SnapshotsController.Get), created.ActionName);
        Assert.NotNull(snapshotService.LastCreateCommand);
        Assert.Equal(new DateOnly(2026, 8, 9), snapshotService.LastCreateCommand!.SnapshotDate);
        Assert.True(snapshotService.LastCreateCommand.AccountIdToBalanceMap.TryGetValue(42, out var balance));
        Assert.Equal(-125.50m, balance);
    }

    private sealed class RecordingFinanceSnapshotService : IFinanceSnapshotService
    {
        public CreateFinanceSnapshotCommand? LastCreateCommand { get; private set; }

        public Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FinanceSnapshot>>([]);

        public Task<FinanceSnapshot?> GetSnapshotByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<FinanceSnapshot?>(null);

        public Task<Result<FinanceSnapshot>> CreateSnapshotAsync(CreateFinanceSnapshotCommand command, CancellationToken cancellationToken = default)
        {
            LastCreateCommand = command;
            return Task.FromResult(Result<FinanceSnapshot>.Success(new FinanceSnapshot(command.SnapshotDate, [CreateBalance(command.AccountIdToBalanceMap)])));
        }

        public Task<Result> UpdateSnapshotAsync(UpdateFinanceSnapshotCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());

        public Task<Result> DeleteSnapshotAsync(int snapshotId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());

        private static AccountBalance CreateBalance(IReadOnlyDictionary<int, decimal> accountIdToBalanceMap)
        {
            var accountId = accountIdToBalanceMap.Keys.First();
            var balance = accountIdToBalanceMap[accountId];
            var account = new Account(accountId, "Test Account", new AccountType("Test Type", new AccountTypeGroup("Investments")), 1);

            return new AccountBalance(account, balance);
        }
    }

    private sealed class RecordingInstitutionService : IInstitutionService
    {
        public Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Institution>>([]);

        public Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Institution?>(null);

        public Task<Result<Institution>> CreateInstitutionAsync(CreateInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Institution>.Failure("Not implemented in test."));

        public Task<Result> UpdateInstitutionAsync(UpdateInstitutionCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));

        public Task<Result> DeleteInstitutionAsync(int institutionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("Not implemented in test."));
    }
}