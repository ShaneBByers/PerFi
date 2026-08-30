namespace PerFi.API.Responses;

public sealed record TransactionResponse(
    int Id,
    DateOnly Date,
    string CounterpartyName,
    decimal Amount,
    string? Description,
    TransactionCategoryIdentityResponse Category,
    AccountIdentityResponse Account);