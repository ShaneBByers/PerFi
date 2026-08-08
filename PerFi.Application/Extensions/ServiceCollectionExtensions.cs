
using Microsoft.Extensions.DependencyInjection;
using PerFi.Application.Interfaces;
using PerFi.Application.Services;

namespace PerFi.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPerFiApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAccountTypeService, AccountTypeService>();
        services.AddScoped<IInstitutionService, InstitutionService>();
        services.AddScoped<IFinanceSnapshotService, FinanceSnapshotService>();

        return services;
    }
}