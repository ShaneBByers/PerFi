namespace PerFi.API.Requests;

public record UpdateFinanceSnapshotRequest(
    DateOnly SnapshotDate,
    IReadOnlyDictionary<int, decimal> AccountIdToBalanceMap);