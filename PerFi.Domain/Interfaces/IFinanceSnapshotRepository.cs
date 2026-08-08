using PerFi.Domain.Entities;

namespace PerFi.Domain.Interfaces;

public interface IFinanceSnapshotRepository
{
    Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default);
    Task<bool> AddSnapshotAsync(FinanceSnapshot snapshot, CancellationToken cancellationToken = default);
}