
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
        services.AddScoped<IContributionContributorService, ContributionContributorService>();
        services.AddScoped<IContributionService, ContributionService>();
        services.AddScoped<ITransactionCategoryService, TransactionCategoryService>();
        services.AddScoped<ITransactionCategoryGroupService, TransactionCategoryGroupService>();
        services.AddScoped<IAccountTypeGroupService, AccountTypeGroupService>();
        services.AddScoped<IAccountTypeService, AccountTypeService>();
        services.AddScoped<IInstitutionService, InstitutionService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IFinanceSnapshotService, FinanceSnapshotService>();

        return services;
    }
}