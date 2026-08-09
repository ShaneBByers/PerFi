namespace PerFi.Application.Commands;

public record CreateAccountTypeCommand(
    string AccountTypeName,
    int AccountTypeGroupId);