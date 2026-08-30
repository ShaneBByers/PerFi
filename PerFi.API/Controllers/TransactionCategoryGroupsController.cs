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
public class TransactionCategoryGroupsController(
    ITransactionCategoryGroupService transactionCategoryGroupService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var groups = await transactionCategoryGroupService.GetAllTransactionCategoryGroupsAsync(HttpContext.RequestAborted);
        return Ok(groups.Select(group => new TransactionCategoryGroupResponse(group.Id, group.Name, group.DisplayOrder)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var group = await transactionCategoryGroupService.GetTransactionCategoryGroupByIdAsync(id, HttpContext.RequestAborted);

        if (group is null)
            return NotFound(new { error = $"No transaction category group found with ID '{id}'." });

        return Ok(new TransactionCategoryGroupResponse(group.Id, group.Name, group.DisplayOrder));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionCategoryGroupRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateTransactionCategoryGroupRequest(request.Name);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await transactionCategoryGroupService.CreateTransactionCategoryGroupAsync(
            new CreateTransactionCategoryGroupCommand(request.Name),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create transaction category group.",
                Detail = result.Error
            });

        var group = result.Value!;
        return CreatedAtAction(nameof(Get), new { id = group.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTransactionCategoryGroupRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateTransactionCategoryGroupRequest(request.Name);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await transactionCategoryGroupService.UpdateTransactionCategoryGroupAsync(
            new UpdateTransactionCategoryGroupCommand(id, request.Name),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await transactionCategoryGroupService.DeleteTransactionCategoryGroupAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderTransactionCategoryGroupsRequest request)
    {
        var result = await transactionCategoryGroupService.ReorderTransactionCategoryGroupsAsync(
            new ReorderTransactionCategoryGroupsCommand(request.OrderedTransactionCategoryGroupIds),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}