using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Responses;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities.Projections;

namespace PerFi.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NetWorthProjectionController(INetWorthProjectionService netWorthProjectionService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var periods = await netWorthProjectionService.GetProjectionAsync(HttpContext.RequestAborted);

        return Ok(periods.Select(ToResponse));
    }

    private static ProjectionPeriodResponse ToResponse(ProjectionPeriod period) => new(
        period.PeriodType,
        period.PeriodStart,
        period.PeriodEnd,
        period.CalendarYear,
        period.Quarter,
        period.IsProjected,
        period.UserAgeAtStart,
        period.UserAgeAtEnd,
        period.FractionalAge,
        period.SalaryAtStart,
        period.SalaryAtEnd,
        period.Groups.Select(ToResponse).ToList(),
        ToResponse(period.Total));

    private static ProjectionGroupResponse ToResponse(ProjectionGroup group) => new(
        group.GroupName,
        ToResponse(group.Amounts),
        ToResponse(group.AmountsInTodaysDollars),
        group.ExpectedAmounts is null ? null : ToResponse(group.ExpectedAmounts),
        group.ExpectedAmountsInTodaysDollars is null ? null : ToResponse(group.ExpectedAmountsInTodaysDollars));

    private static ProjectionAmountsResponse ToResponse(ProjectionAmounts amounts) => new(
        amounts.StartingAmount,
        amounts.ContributedAmount,
        amounts.GrowthAmount,
        amounts.FinalAmount);
}
