namespace PerFi.API.Requests;

public sealed record UpdateTransactionCategoryRequest(
    string Name,
    int TransactionCategoryGroupId);