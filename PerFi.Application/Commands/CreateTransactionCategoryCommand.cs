namespace PerFi.Application.Commands;

public sealed record CreateTransactionCategoryCommand(
    string Name,
    int TransactionCategoryGroupId);