namespace PerFi.Application.Commands;

public record CreateSalaryProgressionCommand(
    DateOnly EffectiveDate,
    decimal AnnualSalary);
