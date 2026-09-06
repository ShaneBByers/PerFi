namespace PerFi.Application.Services.Projection;

internal static class LinearBalanceInterpolator
{
    // points must be sorted ascending by Date with no duplicate dates.
    public static decimal Interpolate(IReadOnlyList<(DateOnly Date, decimal Balance)> points, DateOnly targetDate)
    {
        if (points.Count == 0)
            throw new ArgumentException("At least one balance point is required.", nameof(points));

        if (targetDate <= points[0].Date)
            return points[0].Balance;

        if (targetDate >= points[^1].Date)
            return points[^1].Balance;

        for (var i = 0; i < points.Count - 1; i++)
        {
            var (startDate, startBalance) = points[i];
            var (endDate, endBalance) = points[i + 1];

            if (targetDate < startDate || targetDate > endDate)
                continue;

            var totalDays = endDate.DayNumber - startDate.DayNumber;
            if (totalDays == 0)
                return endBalance;

            var elapsedDays = targetDate.DayNumber - startDate.DayNumber;
            var fraction = (decimal)elapsedDays / totalDays;

            return startBalance + ((endBalance - startBalance) * fraction);
        }

        return points[^1].Balance;
    }
}
