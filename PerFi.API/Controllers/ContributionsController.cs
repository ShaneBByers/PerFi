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
[Route("api/[controller]")]
public class ContributionsController(
    IContributionService contributionService,
    IAccountService accountService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var contributions = await contributionService.GetAllContributionsAsync(HttpContext.RequestAborted);
        var accounts = await accountService.GetAllAccountsAsync(HttpContext.RequestAborted);
        var accountNameById = accounts.ToDictionary(account => account.Id, account => account.Name);

        return Ok(contributions.Select(contribution => new ContributionResponse(
            contribution.Id,
            contribution.Date,
            contribution.Amount,
            new ContributionContributorIdentityResponse(contribution.Contributor.Id, contribution.Contributor.Name),
            new AccountIdentityResponse(contribution.AccountId, accountNameById.GetValueOrDefault(contribution.AccountId, "Unknown Account")))));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var contribution = await contributionService.GetContributionByIdAsync(id, HttpContext.RequestAborted);

        if (contribution is null)
            return NotFound(new { error = $"No contribution found with ID '{id}'." });

        var accounts = await accountService.GetAllAccountsAsync(HttpContext.RequestAborted);
        var accountNameById = accounts.ToDictionary(account => account.Id, account => account.Name);

        return Ok(new ContributionResponse(
            contribution.Id,
            contribution.Date,
            contribution.Amount,
            new ContributionContributorIdentityResponse(contribution.Contributor.Id, contribution.Contributor.Name),
            new AccountIdentityResponse(contribution.AccountId, accountNameById.GetValueOrDefault(contribution.AccountId, "Unknown Account"))));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContributionRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateContributionRequest(request.Date, request.Amount, request.ContributionContributorId, request.AccountId);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await contributionService.CreateContributionAsync(
            new CreateContributionCommand(request.Date, request.Amount, request.ContributionContributorId, request.AccountId),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create contribution.",
                Detail = result.Error
            });

        var contribution = result.Value!;
        return CreatedAtAction(nameof(Get), new { id = contribution.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateContributionRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateContributionRequest(request.Date, request.Amount, request.ContributionContributorId, request.AccountId);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await contributionService.UpdateContributionAsync(
            new UpdateContributionCommand(id, request.Date, request.Amount, request.ContributionContributorId, request.AccountId),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await contributionService.DeleteContributionAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}