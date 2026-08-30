namespace PerFi.Application.Commands;

public sealed record UpdateTransactionCommand(
    int TransactionId,
    DateOnly Date,
    string CounterpartyName,
    decimal Amount,
    int TransactionCategoryId,
    int AccountId,
    string? Description);