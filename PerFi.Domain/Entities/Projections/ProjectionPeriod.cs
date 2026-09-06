namespace PerFi.Domain.Entities.Projections;

// Quarter is null for Annual periods. IsProjected is true when PeriodEnd is after "today".
public sealed record ProjectionPeriod(
    ProjectionPeriodType PeriodType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int CalendarYear,
    int? Quarter,
    bool IsProjected,
    int UserAgeAtStart,
    int UserAgeAtEnd,
    decimal SalaryAtStart,
    decimal SalaryAtEnd,
    IReadOnlyList<ProjectionGroup> Groups,
    ProjectionGroup Total);
