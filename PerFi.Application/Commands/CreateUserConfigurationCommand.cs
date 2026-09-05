using PerFi.Domain.Entities;

namespace PerFi.Application.Commands;

public record CreateUserConfigurationCommand(
    DateOnly BirthDate,
    PayCycleType PayCycleType,
    DateOnly ReferencePayDate,
    decimal ExpectedAnnualSalaryRaisePercentage,
    decimal ExpectedAnnualInflationPercentage);
