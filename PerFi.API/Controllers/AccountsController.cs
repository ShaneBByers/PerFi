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
public class AccountsController(
    IAccountService accountService,
    IInstitutionService institutionService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var accounts = await accountService.GetAllAccountsAsync(HttpContext.RequestAborted);
        var institutions = await institutionService.GetAllInstitutionsAsync(HttpContext.RequestAborted);
        var institutionNameById = institutions.ToDictionary(i => i.Id, i => i.Name);

        var response = accounts.Select(a => new AccountResponse(
            a.Id, 
            a.Name,
            new InstitutionIdentityResponse(
                a.InstitutionId,
                institutionNameById.GetValueOrDefault(a.InstitutionId, "Unknown Institution")),
            new AccountTypeResponse(
                a.Type.Id,
                a.Type.Name)));
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var account = await accountService.GetAccountByIdAsync(id, HttpContext.RequestAborted);

        if (account is null)
            return NotFound(new { error = $"No account found with ID '{id}'." });

        var institution = await institutionService.GetInstitutionByIdAsync(account.InstitutionId, HttpContext.RequestAborted);
        if (institution is null)
            return NotFound(new { error = $"No institution found with ID '{account.InstitutionId}'." });

        var response = new AccountResponse(
            account.Id, 
            account.Name,
            new InstitutionIdentityResponse(account.InstitutionId, institution.Name),
            new AccountTypeResponse(
                account.Type.Id,
                account.Type.Name));
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateAccountRequest(request.AccountName, request.InstitutionId, request.AccountTypeId);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new CreateAccountCommand(request.AccountName, request.InstitutionId, request.AccountTypeId);
        var result = await accountService.CreateAccountAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create account.",
                Detail = result.Error
            });

        return CreatedAtAction(nameof(Get), new { id = result.Value }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateAccountRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateAccountRequest(request.AccountName, request.InstitutionId, request.AccountTypeId);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new UpdateAccountCommand(id, request.AccountName, request.InstitutionId, request.AccountTypeId);
        var result = await accountService.UpdateAccountAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await accountService.DeleteAccountAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}