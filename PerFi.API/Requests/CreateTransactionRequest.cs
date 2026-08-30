namespace PerFi.API.Requests;

public sealed record CreateTransactionRequest(
    DateOnly Date,
    string CounterpartyName,
    decimal Amount,
    int TransactionCategoryId,
    int AccountId,
    string? Description);