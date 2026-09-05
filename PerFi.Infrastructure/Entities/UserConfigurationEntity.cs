namespace PerFi.Infrastructure.Entities;

public class UserConfigurationEntity
{
    public int Id { get; set; }
    public DateOnly BirthDate { get; set; }
    public TimeSpan PayCycle { get; set; }
    public decimal CurrentAnnualSalary { get; set; }
    public DateTimeOffset LastVerifiedDateTime { get; set; }
    public required string UserId { get; set; }
    public UserConfigurationExpectationsEntity UserExpectations { get; set; } = null!;
}
