using Microsoft.AspNetCore.Mvc;
using PerFi.API.Requests;

namespace PerFi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var accounts = new[]
        {
            new { Id = 1, Name = "Checking", Balance = 1000m },
            new { Id = 2, Name = "Savings", Balance = 5000m }
        };

        return Ok(accounts);
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var account = new { Id = id, Name = "Checking", Balance = 1000m };
        return Ok(account);
    }

    [HttpPost]
    public IActionResult Create([FromBody] AccountCreateRequest request)
    {
        var created = new { Id = 3, request.Name, request.InitialBalance };
        return CreatedAtAction(nameof(GetById), new { id = 3 }, created);
    }
}