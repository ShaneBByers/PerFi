using PerFi.Domain.Entities;

namespace PerFi.Application.Services.Projection;

internal static class PayDateSequenceGenerator
{
    public static int AnnualPayCycleCount(PayCycleType payCycleType) => payCycleType switch
    {
        PayCycleType.Weekly => 52,
        PayCycleType.BiWeekly => 26,
        PayCycleType.SemiMonthly => 24,
        PayCycleType.Monthly => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(payCycleType))
    };

    public static IReadOnlyList<DateOnly> GeneratePayDates(PayCycleType payCycleType, DateOnly referenceDate, DateOnly rangeStart, DateOnly rangeEnd)
    {
        if (rangeEnd < rangeStart)
            return [];

        return payCycleType switch
        {
            PayCycleType.Weekly => GenerateFixedInterval(referenceDate, rangeStart, rangeEnd, 7),
            PayCycleType.BiWeekly => GenerateFixedInterval(referenceDate, rangeStart, rangeEnd, 14),
            PayCycleType.Monthly => GenerateMonthly(referenceDate, rangeStart, rangeEnd),
            PayCycleType.SemiMonthly => GenerateSemiMonthly(referenceDate, rangeStart, rangeEnd),
            _ => throw new ArgumentOutOfRangeException(nameof(payCycleType))
        };
    }

    private static List<DateOnly> GenerateFixedInterval(DateOnly referenceDate, DateOnly rangeStart, DateOnly rangeEnd, int intervalDays)
    {
        var dates = new List<DateOnly>();
        var daysFromReferenceToStart = rangeStart.DayNumber - referenceDate.DayNumber;
        var index = (int)Math.Floor(daysFromReferenceToStart / (double)intervalDays) - 1;

        while (true)
        {
            var date = referenceDate.AddDays(index * intervalDays);
            if (date > rangeEnd)
                break;
            if (date >= rangeStart)
                dates.Add(date);
            index++;
        }

        return dates;
    }

    private static List<DateOnly> GenerateMonthly(DateOnly referenceDate, DateOnly rangeStart, DateOnly rangeEnd)
    {
        var dates = new List<DateOnly>();
        var monthsFromReferenceToStart = ((rangeStart.Year - referenceDate.Year) * 12) + rangeStart.Month - referenceDate.Month;
        var index = monthsFromReferenceToStart - 1;

        while (true)
        {
            var date = referenceDate.AddMonths(index);
            if (date > rangeEnd)
                break;
            if (date >= rangeStart)
                dates.Add(date);
            index++;
        }

        return dates;
    }

    private static List<DateOnly> GenerateSemiMonthly(DateOnly referenceDate, DateOnly rangeStart, DateOnly rangeEnd)
    {
        var dates = new List<DateOnly>();
        var day = referenceDate.Day;
        var monthsFromReferenceToStart = ((rangeStart.Year - referenceDate.Year) * 12) + rangeStart.Month - referenceDate.Month;
        var index = monthsFromReferenceToStart - 1;

        while (true)
        {
            // AddMonths clamps the day-of-month, so firstDate is naturally this month's "day" pay date.
            var firstDate = referenceDate.AddMonths(index);
            if (firstDate > rangeEnd)
                break;

            var daysInMonth = DateTime.DaysInMonth(firstDate.Year, firstDate.Month);
            var secondDate = new DateOnly(firstDate.Year, firstDate.Month, Math.Min(day + 15, daysInMonth));

            if (firstDate >= rangeStart)
                dates.Add(firstDate);
            if (secondDate != firstDate && secondDate >= rangeStart && secondDate <= rangeEnd)
                dates.Add(secondDate);

            index++;
        }

        dates.Sort();
        return dates;
    }
}
