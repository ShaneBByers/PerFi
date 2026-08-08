using Microsoft.AspNetCore.Mvc;
using PerFi.API.Requests;
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
        return Ok(snapshots);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CreateFinanceSnapshotRequest request)
    {
        var snapshot = new CreateFinanceSnapshotCommand(request.SnapshotDate, request.AccountNameToBalanceMap);
        var created = await financeSnapshotService.CreateSnapshotAsync(snapshot, HttpContext.RequestAborted);
        return Ok(created);
    }
}