namespace PerFi.API.Responses;

public sealed record InstitutionResponse(
    int Id,
    string Name,
    IReadOnlyList<AccountResponse> Accounts);
