namespace PerFi.API.Requests;

public sealed record UpdateTransactionRequest(
    DateOnly Date,
    string CounterpartyName,
    decimal Amount,
    int TransactionCategoryId,
    int AccountId,
    string? Description);