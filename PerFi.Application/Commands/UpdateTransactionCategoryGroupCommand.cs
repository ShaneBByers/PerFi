namespace PerFi.Application.Commands;

public sealed record UpdateTransactionCategoryGroupCommand(
    int TransactionCategoryGroupId,
    string Name);