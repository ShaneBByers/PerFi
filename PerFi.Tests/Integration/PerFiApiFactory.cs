using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PerFi.Infrastructure;

namespace PerFi.Tests.Integration;

public sealed class PerFiApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "development-only-super-secret-key-12345",
                ["Jwt:Issuer"] = "PerFi",
                ["Jwt:Audience"] = "PerFi-Clients",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PerFiDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<PerFiDbContext>();

            services.AddDbContext<PerFiDbContext>(options =>
                options.UseInMemoryDatabase("PerFiTests"));
        });
    }
}
