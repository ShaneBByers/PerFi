using Microsoft.AspNetCore.Mvc;
using PerFi.API.Infrastructure.Authentication;
using PerFi.API.Infrastructure.ExceptionHandling;
using PerFi.API.Infrastructure.HealthChecks;
using PerFi.Bootstrapper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddLogging();
builder.Services.AddPerFiAuthentication(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

builder.Services.AddPerFiBootstrapper(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();