using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface IFinanceSnapshotService
{
    Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default);
    Task<FinanceSnapshot?> GetSnapshotByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<FinanceSnapshot>> CreateSnapshotAsync(CreateFinanceSnapshotCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateSnapshotAsync(UpdateFinanceSnapshotCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateSnapshotCellsAsync(BulkUpdateFinanceSnapshotCellsCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteSnapshotAsync(int snapshotId, CancellationToken cancellationToken = default);
}