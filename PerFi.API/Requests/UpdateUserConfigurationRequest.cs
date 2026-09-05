using PerFi.Domain.Entities;

namespace PerFi.API.Requests;

public record UpdateUserConfigurationRequest(
    DateOnly BirthDate,
    PayCycleType PayCycleType,
    DateOnly ReferencePayDate,
    decimal ExpectedAnnualSalaryRaisePercentage,
    decimal ExpectedAnnualInflationPercentage);
