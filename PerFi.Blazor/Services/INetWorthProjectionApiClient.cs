using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface INetWorthProjectionApiClient
{
    Task<IReadOnlyList<ProjectionPeriodResponse>> GetProjectionAsync(CancellationToken cancellationToken = default);
}
