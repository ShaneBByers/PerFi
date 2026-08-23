using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace PerFi.API.Infrastructure.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddPerFiAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var issuer = jwtSettings["Issuer"] ?? "PerFi";
        var audience = jwtSettings["Audience"] ?? "PerFi-Clients";
        var expiryMinutes = int.TryParse(jwtSettings["ExpiryMinutes"], out var parsedExpiry) ? parsedExpiry : 60;
        var keyVaultUri = jwtSettings["KeyVaultUri"];
        var keyName = jwtSettings["KeyName"];

        SecurityKey validationKey;

        if (!string.IsNullOrWhiteSpace(keyVaultUri) && !string.IsNullOrWhiteSpace(keyName))
        {
            // Private key never leaves the vault: fetch the public key for validation, sign remotely for issuance.
            var credential = new DefaultAzureCredential();
            var keyClient = new KeyClient(new Uri(keyVaultUri), credential);
            var signingKey = keyClient.GetKey(keyName).Value;
            var cryptographyClient = keyClient.GetCryptographyClient(signingKey.Name, signingKey.Properties.Version);

            services.AddSingleton<IJwtTokenService>(new KeyVaultJwtTokenService(cryptographyClient, issuer, audience, expiryMinutes));
            validationKey = new RsaSecurityKey(signingKey.Key.ToRSA()) { KeyId = signingKey.Id!.ToString() };
        }
        else
        {
            var key = jwtSettings["Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
                {
                    key = "development-only-super-secret-key-12345";
                }
                else
                {
                    throw new InvalidOperationException("JWT signing is not configured. Set Jwt:KeyVaultUri/Jwt:KeyName (Key Vault signing) or Jwt:Key (symmetric, development/testing only).");
                }
            }

            services.AddSingleton<IJwtTokenService>(new SymmetricJwtTokenService(key, issuer, audience, expiryMinutes));
            validationKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = validationKey,
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }
}
