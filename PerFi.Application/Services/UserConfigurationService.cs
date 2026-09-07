using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class UserConfigurationService(IUserConfigurationRepository userConfigurationRepository)
    : IUserConfigurationService
{
    public async Task<UserConfiguration?> GetUserConfigurationAsync(CancellationToken cancellationToken = default)
        => await userConfigurationRepository.GetUserConfigurationAsync(cancellationToken);

    public async Task<Result<UserConfiguration>> CreateUserConfigurationAsync(CreateUserConfigurationCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<UserConfiguration>.Failure("Create user configuration command cannot be null.");

        var userConfiguration = new UserConfiguration(
            command.BirthDate,
            command.PayCycleType,
            command.ReferencePayDate,
            command.ExpectedAnnualSalaryRaisePercentage,
            command.ExpectedAnnualInflationPercentage,
            command.RetirementAge);

        var result = await userConfigurationRepository.AddUserConfigurationAsync(userConfiguration, cancellationToken);

        if (!result.IsSuccess)
            return Result<UserConfiguration>.Failure(result.Error);

        userConfiguration.Id = result.Value;
        return Result<UserConfiguration>.Success(userConfiguration);
    }

    public async Task<Result> UpdateUserConfigurationAsync(UpdateUserConfigurationCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Update user configuration command cannot be null.");

        var userConfiguration = new UserConfiguration(
            command.UserConfigurationId,
            command.BirthDate,
            command.PayCycleType,
            command.ReferencePayDate,
            command.ExpectedAnnualSalaryRaisePercentage,
            command.ExpectedAnnualInflationPercentage,
            command.RetirementAge);

        return await userConfigurationRepository.UpdateUserConfigurationAsync(userConfiguration, cancellationToken);
    }
}
