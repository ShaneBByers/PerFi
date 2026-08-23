namespace PerFi.API.Infrastructure.Authentication;

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(string userId, string username, CancellationToken cancellationToken = default);
}
