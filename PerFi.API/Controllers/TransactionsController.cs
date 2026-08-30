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
public class TransactionsController(
    ITransactionService transactionService,
    IAccountService accountService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var transactions = await transactionService.GetAllTransactionsAsync(HttpContext.RequestAborted);
        var accounts = await accountService.GetAllAccountsAsync(HttpContext.RequestAborted);
        var accountNameById = accounts.ToDictionary(account => account.Id, account => account.Name);

        return Ok(transactions.Select(transaction => new TransactionResponse(
            transaction.Id,
            transaction.Date,
            transaction.CounterpartyName,
            transaction.Amount,
            transaction.Description,
            new TransactionCategoryIdentityResponse(transaction.Category.Id, transaction.Category.Name, new TransactionCategoryGroupIdentityResponse(transaction.Category.Group.Id, transaction.Category.Group.Name)),
            new AccountIdentityResponse(transaction.AccountId, accountNameById.GetValueOrDefault(transaction.AccountId, "Unknown Account")))));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var transaction = await transactionService.GetTransactionByIdAsync(id, HttpContext.RequestAborted);

        if (transaction is null)
            return NotFound(new { error = $"No transaction found with ID '{id}'." });

        var accounts = await accountService.GetAllAccountsAsync(HttpContext.RequestAborted);
        var accountNameById = accounts.ToDictionary(account => account.Id, account => account.Name);

        return Ok(new TransactionResponse(
            transaction.Id,
            transaction.Date,
            transaction.CounterpartyName,
            transaction.Amount,
            transaction.Description,
            new TransactionCategoryIdentityResponse(transaction.Category.Id, transaction.Category.Name, new TransactionCategoryGroupIdentityResponse(transaction.Category.Group.Id, transaction.Category.Group.Name)),
            new AccountIdentityResponse(transaction.AccountId, accountNameById.GetValueOrDefault(transaction.AccountId, "Unknown Account"))));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        var validationErrors = RequestValidator.ValidateCreateTransactionRequest(request.Date, request.CounterpartyName, request.Amount, request.TransactionCategoryId, request.AccountId);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await transactionService.CreateTransactionAsync(
            new CreateTransactionCommand(request.Date, request.CounterpartyName, request.Amount, request.TransactionCategoryId, request.AccountId, request.Description),
            HttpContext.RequestAborted);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unable to create transaction.",
                Detail = result.Error
            });

        var transaction = result.Value!;
        return CreatedAtAction(nameof(Get), new { id = transaction.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTransactionRequest request)
    {
        var validationErrors = RequestValidator.ValidateUpdateTransactionRequest(request.Date, request.CounterpartyName, request.Amount, request.TransactionCategoryId, request.AccountId);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors.ToValidationProblemDetails());

        var result = await transactionService.UpdateTransactionAsync(
            new UpdateTransactionCommand(id, request.Date, request.CounterpartyName, request.Amount, request.TransactionCategoryId, request.AccountId, request.Description),
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
        var result = await transactionService.DeleteTransactionAsync(id, HttpContext.RequestAborted);

        if (result.IsFailure)
            return IsNotFoundError(result.Error)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private static bool IsNotFoundError(string? error)
        => !string.IsNullOrWhiteSpace(error) && error.Contains("not found", StringComparison.OrdinalIgnoreCase);
}