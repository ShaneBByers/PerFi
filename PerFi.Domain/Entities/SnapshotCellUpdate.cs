namespace PerFi.Domain.Entities;

public sealed record SnapshotCellUpdate(
    int SnapshotId,
    int AccountId,
    decimal Balance);
