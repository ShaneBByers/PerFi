namespace PerFi.Application.Commands;

public record UpdateAccountTypeCommand(
    int AccountTypeId,
    string AccountTypeName,
    int AccountTypeGroupId);