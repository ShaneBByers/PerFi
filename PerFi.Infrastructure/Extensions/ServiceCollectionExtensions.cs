using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerFi.Domain.Interfaces;
using PerFi.Infrastructure.Entities;
using PerFi.Infrastructure.Services;

namespace PerFi.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPerFiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useLocalConnection = configuration.GetValue<bool>("Database:UseLocalConnection");
        var connectionStringName = useLocalConnection ? "LocalConnection" : "DefaultConnection";

        services.AddDbContext<PerFiDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString(connectionStringName),
                sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout(60);
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));

        // Web hosts register this automatically; non-web hosts (e.g. PerFi.Console) need it explicitly
        // for AddDefaultTokenProviders' DataProtectorTokenProvider to resolve IDataProtectionProvider.
        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<PerFiDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountTypeGroupRepository, AccountTypeGroupRepository>();
        services.AddScoped<IAccountTypeRepository, AccountTypeRepository>();
        services.AddScoped<IInstitutionRepository, InstitutionRepository>();
        services.AddScoped<IFinanceSnapshotRepository, FinanceSnapshotRepository>();

        return services;
    }
}