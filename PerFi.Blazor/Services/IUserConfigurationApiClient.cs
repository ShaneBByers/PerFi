using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface IUserConfigurationApiClient
{
    Task<UserConfigurationResponse?> GetAsync(CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(CreateUserConfigurationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, UpdateUserConfigurationRequest request, CancellationToken cancellationToken = default);
}
