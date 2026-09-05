namespace PerFi.Domain.Entities;

public sealed record UserConfiguration
{
    public int Id { get; set; }
    public DateOnly BirthDate { get; }
    public TimeSpan PayCycle { get; }
    public decimal CurrentAnnualSalary { get; }
    public DateTimeOffset LastVerifiedDateTime { get; }
    public UserConfigurationExpectations UserExpectations { get; }

    public UserConfiguration(
        DateOnly birthDate,
        TimeSpan payCycle,
        decimal currentAnnualSalary,
        DateTimeOffset lastVerifiedDateTime,
        UserConfigurationExpectations userExpectations)
    {
        ArgumentNullException.ThrowIfNull(userExpectations, nameof(userExpectations));

        BirthDate = birthDate;
        PayCycle = payCycle;
        CurrentAnnualSalary = currentAnnualSalary;
        LastVerifiedDateTime = lastVerifiedDateTime;
        UserExpectations = userExpectations;
    }

    public UserConfiguration(
        int id,
        DateOnly birthDate,
        TimeSpan payCycle,
        decimal currentAnnualSalary,
        DateTimeOffset lastVerifiedDateTime,
        UserConfigurationExpectations userExpectations)
        : this(birthDate, payCycle, currentAnnualSalary, lastVerifiedDateTime, userExpectations)
    {
        Id = id;
    }
}