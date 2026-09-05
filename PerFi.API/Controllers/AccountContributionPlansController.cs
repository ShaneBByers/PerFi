using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Requests;
using PerFi.API.Responses;
using PerFi.API.Validation;
using PerFi.Application.Commands;
using PerFi.Application.Interfaces;

namespace PerFi.API.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts/{accountId}/contribution-plans")]
public class AccountContributionPlansController(IAccountContributionPlanService accountContributionPlanService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromRoute] int accountId)
    {
        var plans = await accountContributionPlanService.GetAllByAccountIdAsync(accountId, HttpContext.RequestAborted);

        return Ok(plans.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int accountId, [FromRoute] int id)
    {
        var plan = await accountContributionPlanService.GetByIdAsync(id, HttpContext.RequestAborted);

        if (plan is null || plan.AccountId != accountId)
            return NotFound(new { error = $"No contribution plan found with ID '{id}' for account '{accountId}'." });

        return Ok(ToResponse(plan));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromRoute] int accountId, [FromBody] CreateAccountContributionPlanRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateAccountContributionPlanRequest(request);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new CreateAccountContributionPlanCommand(
            accountId,
            request.ContributorType,
            request.DollarAmountPerPayCycle,
            request.DollarAmountPerPayCycleAnnualIncrease,
            request.DollarAmountAnnual,
            request.DollarAmountAnnualIncrease,
            request.PercentagePerPayCycle,
            request.PercentagePerPayCycleAnnualIncrease,
            request.PercentageAnnual,
            request.PercentageAnnualIncrease);

        var result = await accountContributionPlanService.CreateAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create account contribution plan.",
                Detail = result.Error
            });

        var plan = result.Value!;
        return CreatedAtAction(nameof(Get), new { accountId, id = plan.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int accountId, [FromRoute] int id, [FromBody] UpdateAccountContributionPlanRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateAccountContributionPlanRequest(request);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new UpdateAccountContributionPlanCommand(
            id,
            accountId,
            request.ContributorType,
            request.DollarAmountPerPayCycle,
            request.DollarAmountPerPayCycleAnnualIncrease,
            request.DollarAmountAnnual,
            request.DollarAmountAnnualIncrease,
            request.PercentagePerPayCycle,
            request.PercentagePerPayCycleAnnualIncrease,
            request.PercentageAnnual,
            request.PercentageAnnualIncrease);

        var result = await accountContributionPlanService.UpdateAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int accountId, [FromRoute] int id)
    {
        var result = await accountContributionPlanService.DeleteAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static AccountContributionPlanResponse ToResponse(PerFi.Domain.Entities.AccountContributionPlan plan) => new(
        plan.Id,
        plan.AccountId,
        plan.ContributorType,
        plan.DollarAmountPerPayCycle,
        plan.DollarAmountPerPayCycleAnnualIncrease,
        plan.DollarAmountAnnual,
        plan.DollarAmountAnnualIncrease,
        plan.PercentagePerPayCycle,
        plan.PercentagePerPayCycleAnnualIncrease,
        plan.PercentageAnnual,
        plan.PercentageAnnualIncrease);

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}
