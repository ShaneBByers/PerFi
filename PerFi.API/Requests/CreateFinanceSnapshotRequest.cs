namespace PerFi.API.Requests;

public record CreateFinanceSnapshotRequest(
    DateOnly SnapshotDate,
    IReadOnlyDictionary<string, double> AccountNameToBalanceMap);