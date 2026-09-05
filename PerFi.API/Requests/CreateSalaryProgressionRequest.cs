namespace PerFi.API.Requests;

public record CreateSalaryProgressionRequest(
    DateOnly EffectiveDate,
    decimal AnnualSalary);
