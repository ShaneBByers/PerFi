using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Infrastructure.Authentication;
using PerFi.API.Requests;

namespace PerFi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IJwtTokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (request.Username == "demo" && request.Password == "demo")
        {
            var token = await tokenService.GenerateTokenAsync(request.Username, cancellationToken);
            return Ok(new { token });
        }

        return Unauthorized(new { error = "Invalid username or password." });
    }
}
