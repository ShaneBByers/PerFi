using Microsoft.AspNetCore.Mvc;
using PerFi.API.Requests;
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
        return Ok(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        var command = new CreateAccountCommand(request.AccountName, request.InstitutionName, request.AccountType);
        var created = await accountService.CreateAccountAsync(command, HttpContext.RequestAborted);
        return Ok(created);
    }
}