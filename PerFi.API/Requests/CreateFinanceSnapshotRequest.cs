namespace PerFi.API.Requests;

public record CreateFinanceSnapshotRequest(
    DateOnly SnapshotDate,
    IReadOnlyDictionary<int, decimal> AccountIdToBalanceMap);