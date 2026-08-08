using Microsoft.AspNetCore.Mvc;
using PerFi.API.Requests;
using PerFi.API.Responses;
using PerFi.Application.Commands;
using PerFi.Application.Interfaces;

namespace PerFi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController(
    IAccountService accountService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var accounts = await accountService.GetAllAccountsAsync(HttpContext.RequestAborted);
        var response = accounts.Select(a => new AccountResponse(
            a.Id, 
            a.Name,
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

        var response = new AccountResponse(
            account.Id, 
            account.Name, 
            new AccountTypeResponse(
                account.Type.Id,
                account.Type.Name));
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        var command = new CreateAccountCommand(request.AccountName, request.InstitutionId, request.AccountTypeId);
        var result = await accountService.CreateAccountAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(Get), new { id = result.Value }, null);
    }
}