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
public class AccountTypesController(
    IAccountTypeService accountTypeService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var accountTypes = await accountTypeService.GetAllAccountTypesAsync(HttpContext.RequestAborted);
        var response = accountTypes.Select(at => new AccountTypeResponse(at.Id, at.Name));
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var accountType = await accountTypeService.GetAccountTypeByIdAsync(id, HttpContext.RequestAborted);

        if (accountType is null)
            return NotFound(new { error = $"No account type found with ID '{id}'." });

        var response = new AccountTypeResponse(accountType.Id, accountType.Name);
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountTypeRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateAccountTypeRequest(request.Name);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new CreateAccountTypeCommand(request.Name);
        var result = await accountTypeService.CreateAccountTypeAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create account type.",
                Detail = result.Error
            });

        return CreatedAtAction(nameof(Get), new { id = result.Value }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateAccountTypeRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateAccountTypeRequest(request.Name);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new UpdateAccountTypeCommand(id, request.Name);
        var result = await accountTypeService.UpdateAccountTypeAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await accountTypeService.DeleteAccountTypeAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}