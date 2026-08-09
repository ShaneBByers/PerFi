namespace PerFi.Application.Commands;

public record UpdateFinanceSnapshotCommand(
    int SnapshotId,
    DateOnly SnapshotDate,
    IReadOnlyDictionary<int, decimal> AccountIdToBalanceMap);