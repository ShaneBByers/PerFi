namespace PerFi.Infrastructure.Services;

public interface IRefreshTokenService
{
    Task<string> IssueAsync(string userId, CancellationToken cancellationToken = default);

    Task<RefreshTokenResult> RedeemAsync(string presentedToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(string presentedToken, CancellationToken cancellationToken = default);
}

public sealed record RefreshTokenResult(bool Succeeded, string? UserId, string? NewRefreshToken, string? FailureReason)
{
    public static RefreshTokenResult Success(string userId, string newRefreshToken) => new(true, userId, newRefreshToken, null);

    public static RefreshTokenResult Failure(string reason) => new(false, null, null, reason);
}
