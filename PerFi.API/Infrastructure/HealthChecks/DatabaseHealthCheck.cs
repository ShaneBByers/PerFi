using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PerFi.API.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck(PerFi.Infrastructure.PerFiDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database is unavailable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unavailable.", ex);
        }
    }
}
