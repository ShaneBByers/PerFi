using PerFi.Domain.Entities;

namespace PerFi.Infrastructure.Entities;

public class UserConfigurationEntity
{
    public int Id { get; set; }
    public DateOnly BirthDate { get; set; }
    public PayCycleType PayCycleType { get; set; }
    public DateOnly ReferencePayDate { get; set; }
    public decimal ExpectedAnnualSalaryRaisePercentage { get; set; }
    public decimal ExpectedAnnualInflationPercentage { get; set; }
    public required string UserId { get; set; }
}
