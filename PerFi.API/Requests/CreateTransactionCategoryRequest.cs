namespace PerFi.API.Requests;

public sealed record CreateTransactionCategoryRequest(
    string Name,
    int TransactionCategoryGroupId);