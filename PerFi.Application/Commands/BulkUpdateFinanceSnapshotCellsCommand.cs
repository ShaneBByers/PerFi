namespace PerFi.Application.Commands;

public sealed record BulkUpdateFinanceSnapshotCellsCommand(
    IReadOnlyList<SnapshotCellUpdateCommand> Updates);

public sealed record SnapshotCellUpdateCommand(
    int SnapshotId,
    int AccountId,
    decimal Balance);
