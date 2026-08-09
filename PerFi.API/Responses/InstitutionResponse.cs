namespace PerFi.API.Responses;

public sealed record InstitutionResponse(
    int Id,
    string Name,
    int DisplayOrder,
    IReadOnlyList<AccountResponse> Accounts);
