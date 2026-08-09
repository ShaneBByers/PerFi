namespace PerFi.API.Responses;

public sealed record AccountTypeGroupResponse(
    int Id,
    string Name,
    int DisplayOrder);