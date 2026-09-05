using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IUserConfigurationExpectationsRepository
{
    Task<UserConfigurationExpectations?> GetUserConfigurationExpectationsAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> AddUserConfigurationExpectationsAsync(UserConfigurationExpectations expectations, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserConfigurationExpectationsAsync(UserConfigurationExpectations expectations, CancellationToken cancellationToken = default);
}