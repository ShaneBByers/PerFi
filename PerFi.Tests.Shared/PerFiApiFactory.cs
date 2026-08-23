using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;

namespace PerFi.Tests.Shared;

public sealed class PerFiApiFactory : WebApplicationFactory<Program>
{
    public const string TestUsername = "test-user";
    public const string TestPassword = "Test-Password1!";

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
            services.RemoveAll<IDbContextOptionsConfiguration<PerFiDbContext>>();

            services.AddDbContext<PerFiDbContext>(options =>
                options.UseInMemoryDatabase("PerFiTests"));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (userManager.FindByNameAsync(TestUsername).GetAwaiter().GetResult() is null)
        {
            var result = userManager.CreateAsync(new ApplicationUser { UserName = TestUsername }, TestPassword)
                .GetAwaiter().GetResult();

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed test user: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
        }

        return host;
    }
}
