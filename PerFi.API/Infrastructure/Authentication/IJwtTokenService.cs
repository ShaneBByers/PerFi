namespace PerFi.API.Infrastructure.Authentication;

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(string username, CancellationToken cancellationToken = default);
}
