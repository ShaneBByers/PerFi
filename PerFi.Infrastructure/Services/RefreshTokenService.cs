using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal sealed class RefreshTokenService(
    PerFiDbContext dbContext,
    IConfiguration configuration,
    TimeProvider timeProvider) : IRefreshTokenService
{
    public async Task<string> IssueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var (token, tokenHash) = GenerateToken();

        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(GetExpiryDays())
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<RefreshTokenResult> RedeemAsync(string presentedToken, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var presentedHash = Hash(presentedToken);

        var existing = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(refreshToken => refreshToken.TokenHash == presentedHash, cancellationToken);

        if (existing is null)
            return RefreshTokenResult.Failure("Invalid refresh token.");

        if (existing.RevokedAtUtc is not null)
        {
            // A previously-rotated-out token was presented again - treat as a possible theft and kill every
            // active token for this user, forcing a real re-login rather than silently trusting it.
            var activeTokens = await dbContext.RefreshTokens
                .Where(refreshToken => refreshToken.UserId == existing.UserId && refreshToken.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var activeToken in activeTokens)
                activeToken.RevokedAtUtc = now;

            await dbContext.SaveChangesAsync(cancellationToken);
            return RefreshTokenResult.Failure("Refresh token has already been used.");
        }

        if (existing.ExpiresAtUtc <= now)
            return RefreshTokenResult.Failure("Refresh token has expired.");

        existing.RevokedAtUtc = now;

        var (newToken, newTokenHash) = GenerateToken();
        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = existing.UserId,
            TokenHash = newTokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(GetExpiryDays())
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return RefreshTokenResult.Success(existing.UserId, newToken);
    }

    public async Task RevokeAsync(string presentedToken, CancellationToken cancellationToken = default)
    {
        var presentedHash = Hash(presentedToken);
        var existing = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(refreshToken => refreshToken.TokenHash == presentedHash, cancellationToken);

        if (existing is null || existing.RevokedAtUtc is not null)
            return;

        existing.RevokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private int GetExpiryDays() =>
        int.TryParse(configuration["Jwt:RefreshTokenExpiryDays"], out var parsedDays) ? parsedDays : 14;

    private static (string Token, string TokenHash) GenerateToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return (token, Hash(token));
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
