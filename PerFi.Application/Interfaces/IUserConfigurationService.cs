using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface IUserConfigurationService
{
    Task<UserConfiguration?> GetUserConfigurationAsync(CancellationToken cancellationToken = default);
    Task<Result<UserConfiguration>> CreateUserConfigurationAsync(CreateUserConfigurationCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserConfigurationAsync(UpdateUserConfigurationCommand command, CancellationToken cancellationToken = default);
}
