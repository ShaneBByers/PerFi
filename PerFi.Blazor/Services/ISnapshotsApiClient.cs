using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface ISnapshotsApiClient
{
    Task<IReadOnlyList<FinanceSnapshotResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FinanceSnapshotResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(DateOnly snapshotDate, IReadOnlyDictionary<int, decimal> accountIdToBalanceMap, CancellationToken cancellationToken = default);
}
