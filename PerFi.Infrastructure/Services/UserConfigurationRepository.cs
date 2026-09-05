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
            .SingleOrDefaultAsync(
                configuration => configuration.UserId == currentUserService.UserId,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
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
        entity.PayCycleType = userConfiguration.PayCycleType;
        entity.ReferencePayDate = userConfiguration.ReferencePayDate;
        entity.ExpectedAnnualSalaryRaisePercentage = userConfiguration.ExpectedAnnualSalaryRaisePercentage;
        entity.ExpectedAnnualInflationPercentage = userConfiguration.ExpectedAnnualInflationPercentage;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private UserConfigurationEntity CreateEntity(UserConfiguration userConfiguration)
    {
        return new UserConfigurationEntity
        {
            BirthDate = userConfiguration.BirthDate,
            PayCycleType = userConfiguration.PayCycleType,
            ReferencePayDate = userConfiguration.ReferencePayDate,
            ExpectedAnnualSalaryRaisePercentage = userConfiguration.ExpectedAnnualSalaryRaisePercentage,
            ExpectedAnnualInflationPercentage = userConfiguration.ExpectedAnnualInflationPercentage,
            UserId = currentUserService.UserId
        };
    }

    private static UserConfiguration ToDomain(UserConfigurationEntity entity)
    {
        return new UserConfiguration(
            entity.Id,
            entity.BirthDate,
            entity.PayCycleType,
            entity.ReferencePayDate,
            entity.ExpectedAnnualSalaryRaisePercentage,
            entity.ExpectedAnnualInflationPercentage);
    }
}