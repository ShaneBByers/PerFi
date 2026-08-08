namespace PerFi.API.Requests;

public record CreateAccountRequest(
    string AccountName,
    int InstitutionId,
    int AccountTypeId);