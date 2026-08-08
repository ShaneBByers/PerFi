using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PerFi.API.Infrastructure.Authentication;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public string GenerateToken(string username)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT signing key is not configured.");
        var issuer = jwtSettings["Issuer"] ?? "PerFi";
        var audience = jwtSettings["Audience"] ?? "PerFi-Clients";
        var expiryMinutes = int.TryParse(jwtSettings["ExpiryMinutes"], out var parsedExpiry) ? parsedExpiry : 60;

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "User")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
