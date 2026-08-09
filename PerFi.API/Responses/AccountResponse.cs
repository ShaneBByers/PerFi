namespace PerFi.API.Responses;

public sealed record AccountResponse(
    int Id,
    string Name,
    InstitutionIdentityResponse Institution,
    AccountTypeResponse Type);
