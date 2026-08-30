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
public class TransactionCategoriesController(
    ITransactionCategoryService transactionCategoryService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await transactionCategoryService.GetAllTransactionCategoriesAsync(HttpContext.RequestAborted);
        return Ok(categories.Select(category => new TransactionCategoryResponse(
            category.Id,
            category.Name,
            category.DisplayOrder,
            new TransactionCategoryGroupIdentityResponse(category.Group.Id, category.Group.Name))));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var category = await transactionCategoryService.GetTransactionCategoryByIdAsync(id, HttpContext.RequestAborted);

        if (category is null)
            return NotFound(new { error = $"No transaction category found with ID '{id}'." });

        return Ok(new TransactionCategoryResponse(
            category.Id,
            category.Name,
            category.DisplayOrder,
            new TransactionCategoryGroupIdentityResponse(category.Group.Id, category.Group.Name)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionCategoryRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateTransactionCategoryRequest(request.Name, request.TransactionCategoryGroupId);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await transactionCategoryService.CreateTransactionCategoryAsync(
            new CreateTransactionCategoryCommand(request.Name, request.TransactionCategoryGroupId),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create transaction category.",
                Detail = result.Error
            });

        var category = result.Value!;
        return CreatedAtAction(nameof(Get), new { id = category.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTransactionCategoryRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateTransactionCategoryRequest(request.Name, request.TransactionCategoryGroupId);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await transactionCategoryService.UpdateTransactionCategoryAsync(
            new UpdateTransactionCategoryCommand(id, request.Name, request.TransactionCategoryGroupId),
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
        var result = await transactionCategoryService.DeleteTransactionCategoryAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderTransactionCategoriesRequest request)
    {
        var result = await transactionCategoryService.ReorderTransactionCategoriesAsync(
            new ReorderTransactionCategoriesCommand(request.OrderedTransactionCategoryIds),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}