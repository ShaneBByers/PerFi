using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IFinanceSnapshotRepository
{
    Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default);
    Task<FinanceSnapshot?> GetSnapshotByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddSnapshotAsync(FinanceSnapshot snapshot, CancellationToken cancellationToken = default);
}