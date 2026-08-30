namespace PerFi.API.Responses;

public sealed record TransactionCategoryIdentityResponse(
    int Id,
    string Name,
    TransactionCategoryGroupIdentityResponse Group);