using PerFi.Domain.Entities.Projections;

namespace PerFi.API.Responses;

public sealed record ProjectionPeriodResponse(
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
    IReadOnlyList<ProjectionGroupResponse> Groups,
    ProjectionGroupResponse Total);
