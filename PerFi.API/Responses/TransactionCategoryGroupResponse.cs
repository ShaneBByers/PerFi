namespace PerFi.API.Responses;

public sealed record TransactionCategoryGroupResponse(
    int Id,
    string Name,
    int DisplayOrder);