namespace PerFi.API.Requests;

public record CreateFinanceSnapshotRequest(
    DateOnly SnapshotDate,
    IReadOnlyDictionary<int, double> AccountIdToBalanceMap);