using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PerFi.API.Infrastructure.Authentication;
using Xunit;

namespace PerFi.Tests.API.Unit;

public class HttpContextCurrentUserServiceTests
{
    [Fact]
    public void UserId_WithAuthenticatedUser_ReturnsNameIdentifierClaim()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-123")], "TestAuth"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new HttpContextCurrentUserService(accessor);

        Assert.Equal("user-123", service.UserId);
    }

    [Fact]
    public void UserId_WithoutAuthenticatedUser_ThrowsInvalidOperationException()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var service = new HttpContextCurrentUserService(accessor);

        Assert.Throws<InvalidOperationException>(() => service.UserId);
    }

    [Fact]
    public void UserId_WithNoHttpContext_ThrowsInvalidOperationException()
    {
        var accessor = new HttpContextAccessor();
        var service = new HttpContextCurrentUserService(accessor);

        Assert.Throws<InvalidOperationException>(() => service.UserId);
    }
}
