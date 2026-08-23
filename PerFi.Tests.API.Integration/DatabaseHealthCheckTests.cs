using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerFi.API.Infrastructure.HealthChecks;
using PerFi.Infrastructure;
using Xunit;

namespace PerFi.Tests.API.Integration;

public sealed class DatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PerFiDbContext>().UseSqlite(connection).Options;

        await using var dbContext = new PerFiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var healthCheck = new DatabaseHealthCheck(dbContext);

        var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsUnavailable_ReturnsUnhealthy()
    {
        // /dev/null is a file, not a directory, so opening a path beneath it always fails.
        var options = new DbContextOptionsBuilder<PerFiDbContext>()
            .UseSqlite("Data Source=/dev/null/unreachable.db")
            .Options;

        await using var dbContext = new PerFiDbContext(options);
        var healthCheck = new DatabaseHealthCheck(dbContext);

        var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, result.Status);
    }
}
