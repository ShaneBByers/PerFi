namespace PerFi.Application.Commands;

public record CreateAccountCommand(
    string AccountName,
    int InstitutionId,
    int AccountTypeId);