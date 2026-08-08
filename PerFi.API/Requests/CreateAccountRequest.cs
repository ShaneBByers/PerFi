using PerFi.Domain.Entities.Enums;

namespace PerFi.API.Requests;

public record CreateAccountRequest(
    string AccountName,
    string InstitutionName,
    AccountType AccountType);