namespace PerFi.API.Responses;

public sealed record FinanceSnapshotResponse(
    int Id,
    DateOnly Date,
    IReadOnlyList<AccountBalanceResponse> AccountBalances);
