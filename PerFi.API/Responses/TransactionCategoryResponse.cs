namespace PerFi.API.Responses;

public sealed record TransactionCategoryResponse(
    int Id,
    string Name,
    int DisplayOrder,
    TransactionCategoryGroupIdentityResponse Group);