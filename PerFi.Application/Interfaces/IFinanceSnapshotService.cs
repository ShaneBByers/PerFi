using PerFi.Application.Commands;
using PerFi.Domain.Entities;

namespace PerFi.Application.Interfaces;

public interface IFinanceSnapshotService
{
    Task<IReadOnlyList<FinanceSnapshot>> GetAllSnapshotsAsync(CancellationToken cancellationToken = default);
    Task<bool> CreateSnapshotAsync(CreateFinanceSnapshotCommand command, CancellationToken cancellationToken = default);
}