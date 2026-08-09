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
public sealed class AccountTypeGroupsController(
    IAccountTypeGroupService accountTypeGroupService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var accountTypeGroups = await accountTypeGroupService.GetAllAccountTypeGroupsAsync(HttpContext.RequestAborted);
        var response = accountTypeGroups.Select(group => new AccountTypeGroupResponse(group.Id, group.Name));
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var accountTypeGroup = await accountTypeGroupService.GetAccountTypeGroupByIdAsync(id, HttpContext.RequestAborted);

        if (accountTypeGroup is null)
            return NotFound(new { error = $"No account type group found with ID '{id}'." });

        return Ok(new AccountTypeGroupResponse(accountTypeGroup.Id, accountTypeGroup.Name));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountTypeGroupRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateAccountTypeGroupRequest(request.Name);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new CreateAccountTypeGroupCommand(request.Name);
        var result = await accountTypeGroupService.CreateAccountTypeGroupAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create account type group.",
                Detail = result.Error
            });

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateAccountTypeGroupRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateAccountTypeGroupRequest(request.Name);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new UpdateAccountTypeGroupCommand(id, request.Name);
        var result = await accountTypeGroupService.UpdateAccountTypeGroupAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await accountTypeGroupService.DeleteAccountTypeGroupAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}