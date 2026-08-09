namespace PerFi.API.Responses;

public sealed record AccountBalanceResponse(
    int SnapshotId,
    AccountResponse Account,
    decimal Balance);
