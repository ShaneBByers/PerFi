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
public class SnapshotsController(
    IFinanceSnapshotService financeSnapshotService,
    IInstitutionService institutionService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var snapshots = await financeSnapshotService.GetAllSnapshotsAsync(HttpContext.RequestAborted);
        var institutions = await institutionService.GetAllInstitutionsAsync(HttpContext.RequestAborted);
        var institutionNameById = institutions.ToDictionary(i => i.Id, i => i.Name);
        
        var response = snapshots.Select(s => new FinanceSnapshotResponse(
            s.Id,
            s.Date,
            [.. s.AccountBalances.Select(ab => new AccountBalanceResponse(
                s.Id,
                new AccountResponse(
                    ab.Account.Id,
                    ab.Account.Name,
                    ab.Account.DisplayOrder,
                    new InstitutionIdentityResponse(
                        ab.Account.InstitutionId,
                        institutionNameById.GetValueOrDefault(ab.Account.InstitutionId, "Unknown Institution")),
                    new AccountTypeResponse(
                        ab.Account.Type.Id,
                        ab.Account.Type.Name,
                        ab.Account.Type.DisplayOrder,
                        new AccountTypeGroupIdentityResponse(ab.Account.Type.Group.Id, ab.Account.Type.Group.Name, ab.Account.Type.Group.DisplayOrder))),
                ab.Balance))]));

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var snapshot = await financeSnapshotService.GetSnapshotByIdAsync(id, HttpContext.RequestAborted);

        if (snapshot is null)
            return NotFound(new { error = $"No snapshot found with ID '{id}'." });

        var institutions = await institutionService.GetAllInstitutionsAsync(HttpContext.RequestAborted);
        var institutionNameById = institutions.ToDictionary(i => i.Id, i => i.Name);

        var response = new FinanceSnapshotResponse(
            snapshot.Id,
            snapshot.Date,
            [.. snapshot.AccountBalances.Select(ab => new AccountBalanceResponse(
                snapshot.Id,
                new AccountResponse(
                    ab.Account.Id,
                    ab.Account.Name,
                    ab.Account.DisplayOrder,
                    new InstitutionIdentityResponse(
                        ab.Account.InstitutionId,
                        institutionNameById.GetValueOrDefault(ab.Account.InstitutionId, "Unknown Institution")),
                    new AccountTypeResponse(
                        ab.Account.Type.Id,
                        ab.Account.Type.Name,
                        ab.Account.Type.DisplayOrder,
                        new AccountTypeGroupIdentityResponse(ab.Account.Type.Group.Id, ab.Account.Type.Group.Name, ab.Account.Type.Group.DisplayOrder))),
                ab.Balance))]);

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinanceSnapshotRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateFinanceSnapshotRequest(request.SnapshotDate, request.AccountIdToBalanceMap);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new CreateFinanceSnapshotCommand(request.SnapshotDate, request.AccountIdToBalanceMap);
        var result = await financeSnapshotService.CreateSnapshotAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create finance snapshot.",
                Detail = result.Error
            });

        return CreatedAtAction(nameof(Get), new { id = result.Value }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateFinanceSnapshotRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateFinanceSnapshotRequest(request.SnapshotDate, request.AccountIdToBalanceMap);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new UpdateFinanceSnapshotCommand(id, request.SnapshotDate, request.AccountIdToBalanceMap);
        var result = await financeSnapshotService.UpdateSnapshotAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPost("bulk-update-cells")]
    public async Task<IActionResult> BulkUpdateCells([FromBody] BulkUpdateFinanceSnapshotCellsRequest request)
    {
        var validationErrors = RequestValidator.ValidateBulkUpdateFinanceSnapshotCellsRequest(request.Updates);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var command = new BulkUpdateFinanceSnapshotCellsCommand(
            [.. request.Updates.Select(update => new SnapshotCellUpdateCommand(
                update.SnapshotId,
                update.AccountId,
                update.Balance))]);

        var result = await financeSnapshotService.UpdateSnapshotCellsAsync(command, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await financeSnapshotService.DeleteSnapshotAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}