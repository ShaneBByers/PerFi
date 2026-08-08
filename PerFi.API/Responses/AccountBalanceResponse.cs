namespace PerFi.API.Responses;

public sealed record AccountBalanceResponse(
    AccountResponse Account,
    double Balance);
