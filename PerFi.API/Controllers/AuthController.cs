using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Infrastructure.Authentication;
using PerFi.API.Requests;
using PerFi.Infrastructure.Entities;

namespace PerFi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
            return Unauthorized(new { error = "Invalid username or password." });

        if (await userManager.IsLockedOutAsync(user))
            return Unauthorized(new { error = "Account is locked out. Try again later." });

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Unauthorized(new { error = "Invalid username or password." });
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var token = await tokenService.GenerateTokenAsync(user.Id, user.UserName ?? request.Username, cancellationToken);
        return Ok(new { token });
    }
}

