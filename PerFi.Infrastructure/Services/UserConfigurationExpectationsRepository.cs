using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class UserConfigurationExpectationsRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService) : IUserConfigurationExpectationsRepository
{
    public async Task<UserConfigurationExpectations?> GetUserConfigurationExpectationsAsync(
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserConfigurationExpectations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                expectations => expectations.UserId == currentUserService.UserId,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<Result<int>> AddUserConfigurationExpectationsAsync(
        UserConfigurationExpectations expectations,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.UserConfigurations.AnyAsync(
                configuration => configuration.UserId == currentUserService.UserId,
                cancellationToken))
        {
            return Result<int>.Failure("A user configuration must exist before adding expectations.");
        }

        if (await dbContext.UserConfigurationExpectations.AnyAsync(
                existing => existing.UserId == currentUserService.UserId,
                cancellationToken))
        {
            return Result<int>.Failure("User configuration expectations already exist.");
        }

        var entity = CreateEntity(expectations);
        dbContext.UserConfigurationExpectations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateUserConfigurationExpectationsAsync(
        UserConfigurationExpectations expectations,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserConfigurationExpectations
            .SingleOrDefaultAsync(
                existing => existing.Id == expectations.Id
                            && existing.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"User configuration expectations with ID '{expectations.Id}' not found.");

        entity.AnnualSalaryRaisePercentage = expectations.AnnualSalaryRaisePercentage;
        entity.EmployerMatchPercentage = expectations.EmployerMatchPercentage;
        entity.EmployerProfitSharingPercentage = expectations.EmployerProfitSharingPercentage;
        entity.AnnualBrokerageContributionPerPayCycleIncrease = expectations.AnnualBrokerageContributionPerPayCycleIncrease;
        entity.AnnualStockMarketReturnPercentage = expectations.AnnualStockMarketReturnPercentage;
        entity.AnnualInflationPercentage = expectations.AnnualInflationPercentage;
        entity.LastVerifiedDateTime = expectations.LastVerifiedDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private UserConfigurationExpectationsEntity CreateEntity(UserConfigurationExpectations expectations)
    {
        return new UserConfigurationExpectationsEntity
        {
            AnnualSalaryRaisePercentage = expectations.AnnualSalaryRaisePercentage,
            EmployerMatchPercentage = expectations.EmployerMatchPercentage,
            EmployerProfitSharingPercentage = expectations.EmployerProfitSharingPercentage,
            AnnualBrokerageContributionPerPayCycleIncrease = expectations.AnnualBrokerageContributionPerPayCycleIncrease,
            AnnualStockMarketReturnPercentage = expectations.AnnualStockMarketReturnPercentage,
            AnnualInflationPercentage = expectations.AnnualInflationPercentage,
            LastVerifiedDateTime = expectations.LastVerifiedDateTime,
            UserId = currentUserService.UserId
        };
    }

    private static UserConfigurationExpectations ToDomain(UserConfigurationExpectationsEntity entity)
    {
        return new UserConfigurationExpectations(
            entity.Id,
            entity.AnnualSalaryRaisePercentage,
            entity.EmployerMatchPercentage,
            entity.EmployerProfitSharingPercentage,
            entity.AnnualBrokerageContributionPerPayCycleIncrease,
            entity.AnnualStockMarketReturnPercentage,
            entity.AnnualInflationPercentage,
            entity.LastVerifiedDateTime);
    }
}