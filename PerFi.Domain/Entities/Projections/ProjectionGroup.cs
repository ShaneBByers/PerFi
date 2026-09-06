namespace PerFi.Domain.Entities.Projections;

// ExpectedAmounts/ExpectedAmountsInTodaysDollars are populated only for fully-historical periods (see NetWorthProjectionCalculator).
public sealed record ProjectionGroup(
    string GroupName,
    ProjectionAmounts Amounts,
    ProjectionAmounts AmountsInTodaysDollars,
    ProjectionAmounts? ExpectedAmounts,
    ProjectionAmounts? ExpectedAmountsInTodaysDollars);
