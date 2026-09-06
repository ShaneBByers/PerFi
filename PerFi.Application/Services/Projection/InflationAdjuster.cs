using PerFi.Domain.Entities.Projections;

namespace PerFi.Application.Services.Projection;

internal static class InflationAdjuster
{
    // StartingAmount is referenced to periodStart; Contributed/Growth/Final are referenced to periodEnd.
    public static ProjectionAmounts ToTodaysDollars(
        ProjectionAmounts amounts,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal expectedAnnualInflationPercentage,
        DateOnly today)
    {
        var startFactor = InflationFactor(periodStart, today, expectedAnnualInflationPercentage);
        var endFactor = InflationFactor(periodEnd, today, expectedAnnualInflationPercentage);

        var startingAmount = amounts.StartingAmount * startFactor;
        var contributedAmount = amounts.ContributedAmount * endFactor;
        var finalAmount = amounts.FinalAmount * endFactor;

        // GrowthAmount is recomputed as the residual (not amounts.GrowthAmount * endFactor) so the
        // Growth = Final - Starting - Contributed invariant still holds after applying two different factors.
        return new ProjectionAmounts(
            startingAmount,
            contributedAmount,
            finalAmount - startingAmount - contributedAmount,
            finalAmount);
    }

    // A future referenceDate yields a negative exponent (discounts down); a past one yields a positive exponent (inflates up).
    private static decimal InflationFactor(DateOnly referenceDate, DateOnly today, decimal expectedAnnualInflationPercentage)
    {
        var days = today.DayNumber - referenceDate.DayNumber;
        var annualRate = 1 + (expectedAnnualInflationPercentage / 100m);

        return (decimal)Math.Pow((double)annualRate, days / 365.0);
    }
}
