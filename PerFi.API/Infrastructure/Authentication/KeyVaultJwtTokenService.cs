using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace PerFi.API.Infrastructure.Authentication;

// Signs the header.payload digest via Key Vault's Sign operation so the RSA private key never leaves the vault.
public sealed class KeyVaultJwtTokenService(CryptographyClient cryptographyClient, string issuer, string audience, int expiryMinutes) : IJwtTokenService
{
    public async Task<string> GenerateTokenAsync(string username, CancellationToken cancellationToken = default)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "User")
        };

        var header = new JwtHeader
        {
            ["alg"] = "RS256",
            ["typ"] = "JWT"
        };
        var payload = new JwtPayload(issuer, audience, claims, notBefore: null, expires: DateTime.UtcNow.AddMinutes(expiryMinutes));
        var unsignedToken = new JwtSecurityToken(header, payload);
        var signingInput = $"{unsignedToken.EncodedHeader}.{unsignedToken.EncodedPayload}";

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(signingInput));
        var signResult = await cryptographyClient.SignAsync(SignatureAlgorithm.RS256, digest, cancellationToken);

        return $"{signingInput}.{Base64UrlEncoder.Encode(signResult.Signature)}";
    }
}
