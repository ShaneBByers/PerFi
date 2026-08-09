namespace PerFi.API.Requests;

public record UpdateAccountRequest(
    string AccountName,
    int InstitutionId,
    int AccountTypeId);