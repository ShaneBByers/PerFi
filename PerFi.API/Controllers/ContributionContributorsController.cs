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
public class ContributionContributorsController(
    IContributionContributorService contributionContributorService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var contributors = await contributionContributorService.GetAllContributionContributorsAsync(HttpContext.RequestAborted);
        return Ok(contributors.Select(contributor => new ContributionContributorResponse(contributor.Id, contributor.Name, contributor.DisplayOrder)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var contributor = await contributionContributorService.GetContributionContributorByIdAsync(id, HttpContext.RequestAborted);

        if (contributor is null)
            return NotFound(new { error = $"No contribution contributor found with ID '{id}'." });

        return Ok(new ContributionContributorResponse(contributor.Id, contributor.Name, contributor.DisplayOrder));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContributionContributorRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateContributionContributorRequest(request.Name);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await contributionContributorService.CreateContributionContributorAsync(
            new CreateContributionContributorCommand(request.Name),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create contribution contributor.",
                Detail = result.Error
            });

        var contributor = result.Value!;
        return CreatedAtAction(nameof(Get), new { id = contributor.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateContributionContributorRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateContributionContributorRequest(request.Name);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await contributionContributorService.UpdateContributionContributorAsync(
            new UpdateContributionContributorCommand(id, request.Name),
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
        var result = await contributionContributorService.DeleteContributionContributorAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderContributionContributorsRequest request)
    {
        var result = await contributionContributorService.ReorderContributionContributorsAsync(
            new ReorderContributionContributorsCommand(request.OrderedContributionContributorIds),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}