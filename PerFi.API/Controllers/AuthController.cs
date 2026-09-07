using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Infrastructure.Authentication;
using PerFi.API.Requests;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;

namespace PerFi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService tokenService,
    IRefreshTokenService refreshTokenService) : ControllerBase
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
        var refreshToken = await refreshTokenService.IssueAsync(user.Id, cancellationToken);
        return Ok(new { token, refreshToken });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await refreshTokenService.RedeemAsync(request.RefreshToken, cancellationToken);
        if (!result.Succeeded || result.UserId is null || result.NewRefreshToken is null)
            return Unauthorized(new { error = result.FailureReason ?? "Invalid refresh token." });

        var user = await userManager.FindByIdAsync(result.UserId);
        if (user is null)
            return Unauthorized(new { error = "Invalid refresh token." });

        var token = await tokenService.GenerateTokenAsync(user.Id, user.UserName ?? string.Empty, cancellationToken);
        return Ok(new { token, refreshToken = result.NewRefreshToken });
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        return Ok();
    }
}


