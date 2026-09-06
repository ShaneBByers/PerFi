namespace PerFi.API.Responses;

public sealed record AccountResponse(
    int Id,
    string Name,
    int DisplayOrderInGroup,
    InstitutionIdentityResponse Institution,
    AccountTypeResponse Type,
    decimal ExpectedAnnualGrowthPercentage);
