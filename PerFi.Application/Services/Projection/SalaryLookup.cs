using PerFi.Domain.Entities;

namespace PerFi.Application.Services.Projection;

internal static class SalaryLookup
{
    public static decimal GetSalaryAt(
        IReadOnlyList<SalaryProgression> salaryProgressions,
        decimal expectedAnnualSalaryRaisePercentage,
        DateOnly targetDate)
    {
        if (salaryProgressions.Count == 0)
            return 0m;

        var ordered = salaryProgressions.OrderBy(progression => progression.EffectiveDate).ToList();

        SalaryProgression? applicable = null;
        var isLatestRow = false;

        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].EffectiveDate > targetDate)
                break;

            applicable = ordered[i];
            isLatestRow = i == ordered.Count - 1;
        }

        // Clamp flat to the earliest known row when the target date precedes all of them.
        if (applicable is null)
            return ordered[0].AnnualSalary;

        // A gap between two known rows stays flat; escalation only applies beyond the very last known row.
        if (!isLatestRow)
            return applicable.AnnualSalary;

        var yearsBeyond = Math.Max(0, targetDate.Year - applicable.EffectiveDate.Year);
        var growthFactor = (decimal)Math.Pow((double)(1 + (expectedAnnualSalaryRaisePercentage / 100m)), yearsBeyond);

        return applicable.AnnualSalary * growthFactor;
    }
}
