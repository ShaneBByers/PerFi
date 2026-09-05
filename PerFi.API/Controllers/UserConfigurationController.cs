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
public class UserConfigurationController(IUserConfigurationService userConfigurationService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userConfiguration = await userConfigurationService.GetUserConfigurationAsync(HttpContext.RequestAborted);

        if (userConfiguration is null)
            return NotFound(new { error = "No user configuration exists yet." });

        return Ok(ToResponse(userConfiguration));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserConfigurationRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateUserConfigurationRequest(request);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new CreateUserConfigurationCommand(
            request.BirthDate,
            request.PayCycleType,
            request.ReferencePayDate,
            request.ExpectedAnnualSalaryRaisePercentage,
            request.ExpectedAnnualInflationPercentage);

        var result = await userConfigurationService.CreateUserConfigurationAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create user configuration.",
                Detail = result.Error
            });

        return CreatedAtAction(nameof(Get), null, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateUserConfigurationRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateUserConfigurationRequest(request);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new UpdateUserConfigurationCommand(
            id,
            request.BirthDate,
            request.PayCycleType,
            request.ReferencePayDate,
            request.ExpectedAnnualSalaryRaisePercentage,
            request.ExpectedAnnualInflationPercentage);

        var result = await userConfigurationService.UpdateUserConfigurationAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static UserConfigurationResponse ToResponse(PerFi.Domain.Entities.UserConfiguration userConfiguration) => new(
        userConfiguration.Id,
        userConfiguration.BirthDate,
        userConfiguration.PayCycleType,
        userConfiguration.ReferencePayDate,
        userConfiguration.ExpectedAnnualSalaryRaisePercentage,
        userConfiguration.ExpectedAnnualInflationPercentage);

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}
