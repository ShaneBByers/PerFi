namespace PerFi.API.Responses;

public sealed record FinanceSnapshotResponse(
    DateOnly Date,
    IReadOnlyList<AccountBalanceResponse> AccountBalances);
