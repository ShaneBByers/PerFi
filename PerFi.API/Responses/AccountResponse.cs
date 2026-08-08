namespace PerFi.API.Responses;

public sealed record AccountResponse(
    int Id,
    string Name,
    AccountTypeResponse Type);
