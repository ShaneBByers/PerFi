namespace PerFi.API.Requests;

public sealed record BulkUpdateFinanceSnapshotCellsRequest(
    IReadOnlyList<SnapshotCellUpdateRequest> Updates);

public sealed record SnapshotCellUpdateRequest(
    int SnapshotId,
    int AccountId,
    decimal Balance);
