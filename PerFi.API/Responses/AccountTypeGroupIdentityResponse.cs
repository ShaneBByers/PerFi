namespace PerFi.API.Responses;

public sealed record AccountTypeGroupIdentityResponse(
    int Id,
    string Name,
    int DisplayOrder);