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
public class SalaryProgressionsController(ISalaryProgressionService salaryProgressionService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var salaryProgressions = await salaryProgressionService.GetAllSalaryProgressionsAsync(HttpContext.RequestAborted);

        return Ok(salaryProgressions.Select(salaryProgression => new SalaryProgressionResponse(
            salaryProgression.Id,
            salaryProgression.EffectiveDate,
            salaryProgression.AnnualSalary)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var salaryProgression = await salaryProgressionService.GetSalaryProgressionByIdAsync(id, HttpContext.RequestAborted);

        if (salaryProgression is null)
            return NotFound(new { error = $"No salary progression found with ID '{id}'." });

        return Ok(new SalaryProgressionResponse(salaryProgression.Id, salaryProgression.EffectiveDate, salaryProgression.AnnualSalary));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSalaryProgressionRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateSalaryProgressionRequest(request.EffectiveDate, request.AnnualSalary);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await salaryProgressionService.CreateSalaryProgressionAsync(
            new CreateSalaryProgressionCommand(request.EffectiveDate, request.AnnualSalary),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create salary progression.",
                Detail = result.Error
            });

        var salaryProgression = result.Value!;
        return CreatedAtAction(nameof(Get), new { id = salaryProgression.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateSalaryProgressionRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateSalaryProgressionRequest(request.EffectiveDate, request.AnnualSalary);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await salaryProgressionService.UpdateSalaryProgressionAsync(
            new UpdateSalaryProgressionCommand(id, request.EffectiveDate, request.AnnualSalary),
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
        var result = await salaryProgressionService.DeleteSalaryProgressionAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}
