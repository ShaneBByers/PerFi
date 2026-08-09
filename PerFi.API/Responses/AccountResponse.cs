namespace PerFi.API.Responses;

public sealed record AccountResponse(
    int Id,
    string Name,
    int DisplayOrder,
    InstitutionIdentityResponse Institution,
    AccountTypeResponse Type);
