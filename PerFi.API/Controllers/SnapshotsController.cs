using Microsoft.AspNetCore.Mvc;
using PerFi.API.Requests;
using PerFi.API.Responses;
using PerFi.Application.Commands;
using PerFi.Application.Interfaces;

namespace PerFi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SnapshotsController(
    IFinanceSnapshotService financeSnapshotService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var snapshots = await financeSnapshotService.GetAllSnapshotsAsync(HttpContext.RequestAborted);
        
        var response = snapshots.Select(s => new FinanceSnapshotResponse(
            s.Date,
            [.. s.AccountBalances.Select(ab => new AccountBalanceResponse(
                new AccountResponse(
                    ab.Account.Id,
                    ab.Account.Name,
                    new AccountTypeResponse(
                        ab.Account.Type.Id,
                        ab.Account.Type.Name)),
                ab.Balance))]));

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var snapshot = await financeSnapshotService.GetSnapshotByIdAsync(id, HttpContext.RequestAborted);

        if (snapshot is null)
            return NotFound(new { error = $"No snapshot found with ID '{id}'." });

        var response = new FinanceSnapshotResponse(
            snapshot.Date,
            [.. snapshot.AccountBalances.Select(ab => new AccountBalanceResponse(
                new AccountResponse(
                    ab.Account.Id,
                    ab.Account.Name,
                    new AccountTypeResponse(
                        ab.Account.Type.Id,
                        ab.Account.Type.Name)),
                ab.Balance))]);

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinanceSnapshotRequest request)
    {
        var command = new CreateFinanceSnapshotCommand(request.SnapshotDate, request.AccountIdToBalanceMap);
        var result = await financeSnapshotService.CreateSnapshotAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(Get), new { id = result.Value }, null);
    }
}