namespace PerFi.Domain.Entities;

public sealed record SalaryProgression
{
    public int Id { get; set; }
    public DateOnly EffectiveDate { get; }
    public decimal AnnualSalary { get; }

    public SalaryProgression(DateOnly effectiveDate, decimal annualSalary)
    {
        if (effectiveDate == default)
            throw new ArgumentOutOfRangeException(nameof(effectiveDate), "Effective date must be provided.");

        if (annualSalary < 0)
            throw new ArgumentOutOfRangeException(nameof(annualSalary), "Annual salary cannot be negative.");

        EffectiveDate = effectiveDate;
        AnnualSalary = annualSalary;
    }

    public SalaryProgression(int id, DateOnly effectiveDate, decimal annualSalary)
        : this(effectiveDate, annualSalary)
    {
        Id = id;
    }
}
