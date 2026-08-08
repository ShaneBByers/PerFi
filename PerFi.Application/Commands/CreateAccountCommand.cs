using PerFi.Domain.Entities.Enums;

namespace PerFi.Application.Commands;

public record CreateAccountCommand(
    string AccountName,
    string InstitutionName,
    AccountType AccountType);