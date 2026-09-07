using PerFi.Domain.Entities;

namespace PerFi.API.Requests;

public record CreateUserConfigurationRequest(
    DateOnly BirthDate,
    PayCycleType PayCycleType,
    DateOnly ReferencePayDate,
    decimal ExpectedAnnualSalaryRaisePercentage,
    decimal ExpectedAnnualInflationPercentage,
    int RetirementAge);
