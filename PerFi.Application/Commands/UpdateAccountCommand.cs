namespace PerFi.Application.Commands;

public record UpdateAccountCommand(
    int AccountId,
    string AccountName,
    int InstitutionId,
    int AccountTypeId);