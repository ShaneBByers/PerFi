using PerFi.Domain.Entities;

namespace PerFi.Application.Commands;

public record UpdateUserConfigurationCommand(
    int UserConfigurationId,
    DateOnly BirthDate,
    PayCycleType PayCycleType,
    DateOnly ReferencePayDate,
    decimal ExpectedAnnualSalaryRaisePercentage,
    decimal ExpectedAnnualInflationPercentage);
