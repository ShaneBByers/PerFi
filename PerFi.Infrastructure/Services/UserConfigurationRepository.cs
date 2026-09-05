using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class UserConfigurationRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService) : IUserConfigurationRepository
{
    public async Task<UserConfiguration?> GetUserConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserConfigurations
            .AsNoTracking()
            .Include(configuration => configuration.UserExpectations)
            .SingleOrDefaultAsync(
                configuration => configuration.UserId == currentUserService.UserId,
                cancellationToken);

        return entity is null
            ? null
            : new UserConfiguration(
                entity.Id,
                entity.BirthDate,
                entity.PayCycle,
                entity.CurrentAnnualSalary,
                entity.LastVerifiedDateTime,
                new UserConfigurationExpectations(
                    entity.UserExpectations.Id,
                    entity.UserExpectations.AnnualSalaryRaisePercentage,
                    entity.UserExpectations.EmployerMatchPercentage,
                    entity.UserExpectations.EmployerProfitSharingPercentage,
                    entity.UserExpectations.AnnualBrokerageContributionPerPayCycleIncrease,
                    entity.UserExpectations.AnnualStockMarketReturnPercentage,
                    entity.UserExpectations.AnnualInflationPercentage,
                    entity.UserExpectations.LastVerifiedDateTime));
    }

    public async Task<Result<int>> AddUserConfigurationAsync(
        UserConfiguration userConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.UserConfigurations.AnyAsync(
                configuration => configuration.UserId == currentUserService.UserId,
                cancellationToken))
        {
            return Result<int>.Failure("A user configuration already exists.");
        }

        var entity = CreateEntity(userConfiguration);
        dbContext.UserConfigurations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateUserConfigurationAsync(
        UserConfiguration userConfiguration,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserConfigurations
            .FirstOrDefaultAsync(
                configuration => configuration.Id == userConfiguration.Id
                                 && configuration.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"User configuration with ID '{userConfiguration.Id}' not found.");

        entity.BirthDate = userConfiguration.BirthDate;
        entity.PayCycle = userConfiguration.PayCycle;
        entity.CurrentAnnualSalary = userConfiguration.CurrentAnnualSalary;
        entity.LastVerifiedDateTime = userConfiguration.LastVerifiedDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private UserConfigurationEntity CreateEntity(UserConfiguration userConfiguration)
    {
        return new UserConfigurationEntity
        {
            BirthDate = userConfiguration.BirthDate,
            PayCycle = userConfiguration.PayCycle,
            CurrentAnnualSalary = userConfiguration.CurrentAnnualSalary,
            LastVerifiedDateTime = userConfiguration.LastVerifiedDateTime,
            UserId = currentUserService.UserId,
            UserExpectations = new UserConfigurationExpectationsEntity
            {
                AnnualSalaryRaisePercentage = userConfiguration.UserExpectations.AnnualSalaryRaisePercentage,
                EmployerMatchPercentage = userConfiguration.UserExpectations.EmployerMatchPercentage,
                EmployerProfitSharingPercentage = userConfiguration.UserExpectations.EmployerProfitSharingPercentage,
                AnnualBrokerageContributionPerPayCycleIncrease = userConfiguration.UserExpectations.AnnualBrokerageContributionPerPayCycleIncrease,
                AnnualStockMarketReturnPercentage = userConfiguration.UserExpectations.AnnualStockMarketReturnPercentage,
                AnnualInflationPercentage = userConfiguration.UserExpectations.AnnualInflationPercentage,
                LastVerifiedDateTime = userConfiguration.UserExpectations.LastVerifiedDateTime,
                UserId = currentUserService.UserId
            }
        };
    }
}