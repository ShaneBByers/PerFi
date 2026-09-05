namespace PerFi.Infrastructure.Entities;

public class SalaryProgressionEntity
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public decimal AnnualSalary { get; set; }
}
