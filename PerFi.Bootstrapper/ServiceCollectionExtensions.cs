using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerFi.Application.Extensions;
using PerFi.Infrastructure.Extensions;

namespace PerFi.Bootstrapper;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPerFiBootstrapper(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPerFiApplication();
        services.AddPerFiInfrastructure(configuration);

        return services;
    }
}