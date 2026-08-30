using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PerFi.Bootstrapper;
using PerFi.Console;
using PerFi.Console.Operations;
using PerFi.Domain.Interfaces;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;

namespace PerFi.Tests.Console.Integration;

// Wires the real Application + Infrastructure services (same composition as PerFi.Console's Program.cs)
// against a Sqlite in-memory database, so import/export tests exercise the actual production code path
// instead of mocks - schema drift between BackupDocument and the real services fails these tests directly.
public sealed class BackupOperationsTestHost : IAsyncDisposable
{
    public const string Username = "example";
    private const string Password = "Test-Password1!";

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    private BackupOperationsTestHost(SqliteConnection connection, ServiceProvider serviceProvider, IServiceScope scope)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _scope = scope;
    }

    public PerFiDbContext DbContext => _scope.ServiceProvider.GetRequiredService<PerFiDbContext>();

    public ImportBackupOperation ImportOperation => _scope.ServiceProvider.GetRequiredService<ImportBackupOperation>();

    public ExportBackupOperation ExportOperation => _scope.ServiceProvider.GetRequiredService<ExportBackupOperation>();

    public static async Task<BackupOperationsTestHost> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:UseLocalConnection"] = "true",
                ["ConnectionStrings:LocalConnection"] = "Data Source=:memory:"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPerFiBootstrapper(configuration);

        services.RemoveAll<DbContextOptions<PerFiDbContext>>();
        services.RemoveAll<DbContextOptions>();
        services.RemoveAll<PerFiDbContext>();
        services.RemoveAll<IDbContextOptionsConfiguration<PerFiDbContext>>();
        services.AddDbContext<PerFiDbContext>(options => options.UseSqlite(connection));

        services.AddScoped<ExportBackupOperation>();
        services.AddScoped<ImportBackupOperation>();
        services.AddScoped<ConsoleCurrentUserService>();
        services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<ConsoleCurrentUserService>());

        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<PerFiDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var createResult = await userManager.CreateAsync(new ApplicationUser { UserName = Username }, Password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed test user: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }

        var currentUser = scope.ServiceProvider.GetRequiredService<ConsoleCurrentUserService>();
        var user = await userManager.FindByNameAsync(Username) ?? throw new InvalidOperationException("Seeded user was not found.");
        currentUser.UserId = user.Id;

        return new BackupOperationsTestHost(connection, serviceProvider, scope);
    }

    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _serviceProvider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
