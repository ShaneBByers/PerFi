using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PerFi.API.Infrastructure.Authentication;
using PerFi.API.Infrastructure.ExceptionHandling;
using PerFi.API.Infrastructure.HealthChecks;
using PerFi.Bootstrapper;
using PerFi.Domain.Interfaces;
using PerFi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "PerFiFrontend";

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddPerFiAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

builder.Services.AddPerFiBootstrapper(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await ApplyMigrationsWithRetryAsync(app.Services, app.Logger);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();

static async Task ApplyMigrationsWithRetryAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken = default)
{
    const int maxAttempts = 3;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PerFiDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }
        catch (SqlException ex) when (IsTransientSqlTimeout(ex) && attempt < maxAttempts)
        {
            var retryDelay = TimeSpan.FromSeconds(attempt * 5);
            logger.LogWarning(
                ex,
                "Database migration attempt {Attempt}/{MaxAttempts} failed due to SQL timeout. Retrying in {DelaySeconds} seconds.",
                attempt,
                maxAttempts,
                retryDelay.TotalSeconds);
            await Task.Delay(retryDelay, cancellationToken);
        }
    }
}

static bool IsTransientSqlTimeout(SqlException ex)
    => ex.Number == -2
       || ex.Message.Contains("Connection Timeout Expired", StringComparison.OrdinalIgnoreCase)
       || ex.InnerException is Win32Exception { NativeErrorCode: 258 };