using Microsoft.AspNetCore.Mvc;
using PerFi.API.Requests;
using PerFi.API.Responses;
using PerFi.Application.Commands;
using PerFi.Application.Interfaces;

namespace PerFi.API.Controllers;

[ApiController]
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
        var command = new CreateAccountTypeCommand(request.Name);
        var result = await accountTypeService.CreateAccountTypeAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(Get), new { id = result.Value }, null);
    }
}