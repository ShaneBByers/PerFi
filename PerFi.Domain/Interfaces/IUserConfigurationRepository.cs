using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IUserConfigurationRepository
{
    Task<UserConfiguration?> GetUserConfigurationAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> AddUserConfigurationAsync(UserConfiguration userConfiguration, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserConfigurationAsync(UserConfiguration userConfiguration, CancellationToken cancellationToken = default);
}