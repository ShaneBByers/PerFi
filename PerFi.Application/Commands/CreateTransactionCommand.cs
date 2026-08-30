namespace PerFi.Application.Commands;

public sealed record CreateTransactionCommand(
    DateOnly Date,
    string CounterpartyName,
    decimal Amount,
    int TransactionCategoryId,
    int AccountId,
    string? Description);