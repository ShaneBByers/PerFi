using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PerFi.Bootstrapper;
using PerFi.Console;
using PerFi.Console.Import;
using PerFi.Console.Operations;

var contentRootPath = ResolveContentRootPath();
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = contentRootPath
});

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile(Path.Combine(contentRootPath, "appsettings.json"), optional: false, reloadOnChange: false)
    .AddJsonFile(
        Path.Combine(contentRootPath, $"appsettings.{builder.Environment.EnvironmentName}.json"),
        optional: true,
        reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException(
        $"ConnectionStrings:DefaultConnection is missing. Checked content root: {contentRootPath}");
}

var command = ConsoleCommand.Parse(args);

builder.Services.AddPerFiBootstrapper(builder.Configuration);
builder.Services.AddScoped<NetWorthCsvParser>();
builder.Services.AddScoped<ImportNetWorthCsvOperation>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();

try
{
    System.Console.WriteLine($"PerFi.Console starting command: {command.Verb}");

    switch (command.Verb.ToLowerInvariant())
    {
        case "import-net-worth":
        {
            var operation = scope.ServiceProvider.GetRequiredService<ImportNetWorthCsvOperation>();
            await operation.ExecuteAsync(command.CsvPath, command.DryRun);
            break;
        }
        default:
            throw new InvalidOperationException($"Unsupported command '{command.Verb}'.");
    }

    System.Console.WriteLine("PerFi.Console operation completed successfully.");
}
catch (Exception ex)
{
    System.Console.WriteLine($"PerFi.Console operation failed: {ex.Message}");
    throw;
}

static string ResolveContentRootPath()
{
    var currentDirectory = Directory.GetCurrentDirectory();
    var currentDirectoryAppSettings = Path.Combine(currentDirectory, "appsettings.json");

    if (File.Exists(currentDirectoryAppSettings))
    {
        return currentDirectory;
    }

    var projectDirectoryFromBuildOutput = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    var buildOutputAppSettings = Path.Combine(projectDirectoryFromBuildOutput, "appsettings.json");

    if (File.Exists(buildOutputAppSettings))
    {
        return projectDirectoryFromBuildOutput;
    }

    return currentDirectory;
}
