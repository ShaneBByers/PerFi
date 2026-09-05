namespace PerFi.Domain.Entities;

public sealed record UserConfiguration
{
    public int Id { get; set; }
    public DateOnly BirthDate { get; }
    public PayCycleType PayCycleType { get; }
    public DateOnly ReferencePayDate { get; }
    public decimal ExpectedAnnualSalaryRaisePercentage { get; }
    public decimal ExpectedAnnualInflationPercentage { get; }

    public UserConfiguration(
        DateOnly birthDate,
        PayCycleType payCycleType,
        DateOnly referencePayDate,
        decimal expectedAnnualSalaryRaisePercentage,
        decimal expectedAnnualInflationPercentage)
    {
        BirthDate = birthDate;
        PayCycleType = payCycleType;
        ReferencePayDate = referencePayDate;
        ExpectedAnnualSalaryRaisePercentage = expectedAnnualSalaryRaisePercentage;
        ExpectedAnnualInflationPercentage = expectedAnnualInflationPercentage;
    }

    public UserConfiguration(
        int id,
        DateOnly birthDate,
        PayCycleType payCycleType,
        DateOnly referencePayDate,
        decimal expectedAnnualSalaryRaisePercentage,
        decimal expectedAnnualInflationPercentage)
        : this(birthDate, payCycleType, referencePayDate, expectedAnnualSalaryRaisePercentage, expectedAnnualInflationPercentage)
    {
        Id = id;
    }
}