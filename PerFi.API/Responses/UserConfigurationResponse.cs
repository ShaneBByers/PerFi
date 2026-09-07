using PerFi.Domain.Entities;

namespace PerFi.API.Responses;

public sealed record UserConfigurationResponse(
    int Id,
    DateOnly BirthDate,
    PayCycleType PayCycleType,
    DateOnly ReferencePayDate,
    decimal ExpectedAnnualSalaryRaisePercentage,
    decimal ExpectedAnnualInflationPercentage,
    int RetirementAge);
