using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PerFi.API.Infrastructure.Authentication;
using Xunit;

namespace PerFi.Tests.API.Unit;

public class SymmetricJwtTokenServiceTests
{
    [Fact]
    public async Task GenerateTokenAsync_IncludesExpectedClaimsIssuerAndAudience()
    {
        var service = new SymmetricJwtTokenService(
            "development-only-super-secret-key-12345",
            "PerFi",
            "PerFi-Clients",
            60);

        var token = await service.GenerateTokenAsync("user-1", "shane");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("PerFi", jwt.Issuer);
        Assert.Contains("PerFi-Clients", jwt.Audiences);
        Assert.Equal("user-1", jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("shane", jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("User", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task GenerateTokenAsync_SetsExpiryInTheFuture()
    {
        var service = new SymmetricJwtTokenService(
            "development-only-super-secret-key-12345",
            "PerFi",
            "PerFi-Clients",
            60);

        var token = await service.GenerateTokenAsync("user-1", "shane");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }
}
