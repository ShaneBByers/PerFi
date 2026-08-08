using PerFi.Domain.Entities.Enums;

namespace PerFi.Domain.Entities;

public record Account(
    string AccountName,
    string InstitutionName,
    AccountType AccountType);