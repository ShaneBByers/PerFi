namespace PerFi.Application.Services.Projection;

internal static class AccountGrowthSimulator
{
    public static decimal ApplyGrowth(decimal balance, int days, decimal expectedAnnualGrowthPercentage)
    {
        if (days <= 0)
            return balance;

        var annualRate = 1 + (expectedAnnualGrowthPercentage / 100m);
        var growthFactor = (decimal)Math.Pow((double)annualRate, days / 365.0);

        return balance * growthFactor;
    }
}
