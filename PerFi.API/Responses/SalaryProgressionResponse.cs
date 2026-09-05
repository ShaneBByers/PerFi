namespace PerFi.API.Responses;

public sealed record SalaryProgressionResponse(
    int Id,
    DateOnly EffectiveDate,
    decimal AnnualSalary);
