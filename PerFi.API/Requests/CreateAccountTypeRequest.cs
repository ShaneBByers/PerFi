namespace PerFi.API.Requests;

public record CreateAccountTypeRequest(
    string Name,
    int AccountTypeGroupId);