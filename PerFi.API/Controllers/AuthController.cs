using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Infrastructure.Authentication;
using PerFi.API.Requests;

namespace PerFi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(JwtTokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username == "demo" && request.Password == "demo")
        {
            var token = tokenService.GenerateToken(request.Username);
            return Ok(new { token });
        }

        return Unauthorized(new { error = "Invalid username or password." });
    }
}
