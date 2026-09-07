namespace PerFi.Infrastructure.Entities;

public sealed class RefreshTokenEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;

    // Only the SHA-256 hash of the opaque token is stored; the raw token is never persisted.
    public string TokenHash { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
