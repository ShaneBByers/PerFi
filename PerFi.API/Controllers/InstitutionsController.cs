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
public class InstitutionsController(
    IInstitutionService institutionService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var institutions = await institutionService.GetAllInstitutionsAsync(HttpContext.RequestAborted);
        var response = institutions.Select(i => new InstitutionResponse(
            i.Id, 
            i.Name, 
            i.DisplayOrder,
            [.. i.Accounts.Select(a => new AccountResponse(
                a.Id, 
                a.Name,
                a.DisplayOrder,
                new InstitutionIdentityResponse(i.Id, i.Name),
                new AccountTypeResponse(
                    a.Type.Id,
                    a.Type.Name,
                    a.Type.DisplayOrder,
                        new AccountTypeGroupIdentityResponse(a.Type.Group.Id, a.Type.Group.Name, a.Type.Group.DisplayOrder))))]));

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var institution = await institutionService.GetInstitutionByIdAsync(id, HttpContext.RequestAborted);

        if (institution is null)
            return NotFound(new { error = $"No institution found with ID '{id}'." });

        var response = new InstitutionResponse(
            institution.Id, 
            institution.Name, 
            institution.DisplayOrder,
            [.. institution.Accounts.Select(a => new AccountResponse(
                a.Id, 
                a.Name,
                a.DisplayOrder,
                new InstitutionIdentityResponse(institution.Id, institution.Name),
                new AccountTypeResponse(
                    a.Type.Id,
                    a.Type.Name,
                    a.Type.DisplayOrder,
                        new AccountTypeGroupIdentityResponse(a.Type.Group.Id, a.Type.Group.Name, a.Type.Group.DisplayOrder))))]);
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInstitutionRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateInstitutionRequest(request.InstitutionName);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new CreateInstitutionCommand(request.InstitutionName);
        var result = await institutionService.CreateInstitutionAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create institution.",
                Detail = result.Error
            });

        return CreatedAtAction(nameof(Get), new { id = result.Value }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateInstitutionRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateInstitutionRequest(request.InstitutionName);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new UpdateInstitutionCommand(id, request.InstitutionName);
        var result = await institutionService.UpdateInstitutionAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await institutionService.DeleteInstitutionAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderInstitutionsRequest request)
    {
        var result = await institutionService.ReorderInstitutionsAsync(
            new ReorderInstitutionCommand(request.OrderedInstitutionIds),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}