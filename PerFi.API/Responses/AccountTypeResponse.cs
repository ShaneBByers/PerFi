namespace PerFi.API.Responses;

public sealed record AccountTypeResponse(
    int Id,
    string Name,
    int DisplayOrder,
    AccountTypeGroupIdentityResponse Group);
