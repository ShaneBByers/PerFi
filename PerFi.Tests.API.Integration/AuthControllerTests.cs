using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PerFi.API.Controllers;
using PerFi.API.Infrastructure.Authentication;
using PerFi.API.Requests;
using PerFi.Infrastructure.Entities;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.API.Integration;

public sealed class AuthControllerTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PerFi.Infrastructure.PerFiDbContext _dbContext;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    public AuthControllerTests()
    {
        (_userManager, _dbContext, _connection) = IdentityTestHelper.CreateUserManager();
    }

    private async Task<ApplicationUser> CreateUserAsync(string username = "shane", string password = "Test-Password1!")
    {
        var user = new ApplicationUser { UserName = username };
        var result = await _userManager.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        return user;
    }

    private static AuthController CreateController(UserManager<ApplicationUser> userManager, IJwtTokenService tokenService)
        => new(userManager, tokenService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsUnauthorized()
    {
        var controller = CreateController(_userManager, Mock.Of<IJwtTokenService>());

        var result = await controller.Login(new LoginRequest("nobody", "whatever"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorizedAndIncrementsAccessFailedCount()
    {
        var user = await CreateUserAsync();
        var controller = CreateController(_userManager, Mock.Of<IJwtTokenService>());

        var result = await controller.Login(new LoginRequest("shane", "WrongPassword1!"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        var failedCount = await _userManager.GetAccessFailedCountAsync(user);
        Assert.Equal(1, failedCount);
    }

    [Fact]
    public async Task Login_WhenLockedOut_ReturnsUnauthorized()
    {
        var user = await CreateUserAsync();
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(5));

        var controller = CreateController(_userManager, Mock.Of<IJwtTokenService>());

        var result = await controller.Login(new LoginRequest("shane", "Test-Password1!"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndResetsFailedCount()
    {
        var user = await CreateUserAsync();
        await _userManager.AccessFailedAsync(user);

        var tokenService = new Mock<IJwtTokenService>();
        tokenService.Setup(s => s.GenerateTokenAsync(user.Id, "shane", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-token");

        var controller = CreateController(_userManager, tokenService.Object);

        var result = await controller.Login(new LoginRequest("shane", "Test-Password1!"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var failedCount = await _userManager.GetAccessFailedCountAsync(user);
        Assert.Equal(0, failedCount);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
