using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PerFi.Console.Operations;
using PerFi.Infrastructure.Extensions;

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

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException(
        $"ConnectionStrings:DefaultConnection is missing. Checked content root: {contentRootPath}");
}

builder.Services.AddPerFiInfrastructure(builder.Configuration);
builder.Services.AddScoped<FetchInstitutionsOperation>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();

try
{
    System.Console.WriteLine("PerFi.Console starting operation: FetchInstitutionsOperation");

    var operation = scope.ServiceProvider.GetRequiredService<FetchInstitutionsOperation>();
    await operation.ExecuteAsync();

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
