namespace PerFi.Application.Commands;

public record UpdateSalaryProgressionCommand(
    int SalaryProgressionId,
    DateOnly EffectiveDate,
    decimal AnnualSalary);
