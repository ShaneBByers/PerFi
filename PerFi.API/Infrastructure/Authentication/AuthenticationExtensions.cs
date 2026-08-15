using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace PerFi.API.Infrastructure.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddPerFiAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = jwtSettings["Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            {
                key = "development-only-super-secret-key-12345";
            }
            else
            {
                throw new InvalidOperationException("JWT signing key is not configured. Set Jwt:Key in environment configuration.");
            }
        }

        var issuer = jwtSettings["Issuer"] ?? "PerFi";
        var audience = jwtSettings["Audience"] ?? "PerFi-Clients";

        services.AddSingleton<JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
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
