namespace PerFi.Application.Commands;

public sealed record UpdateTransactionCategoryCommand(
    int TransactionCategoryId,
    string Name,
    int TransactionCategoryGroupId);