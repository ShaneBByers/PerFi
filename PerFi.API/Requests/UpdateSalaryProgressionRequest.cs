namespace PerFi.API.Requests;

public record UpdateSalaryProgressionRequest(
    DateOnly EffectiveDate,
    decimal AnnualSalary);
