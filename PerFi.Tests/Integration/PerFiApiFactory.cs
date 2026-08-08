using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PerFi.Infrastructure;

namespace PerFi.Tests.Integration;

public sealed class PerFiApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

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
