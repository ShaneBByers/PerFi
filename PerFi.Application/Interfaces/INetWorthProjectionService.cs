using PerFi.Domain.Entities.Projections;

namespace PerFi.Application.Interfaces;

public interface INetWorthProjectionService
{
    Task<IReadOnlyList<ProjectionPeriod>> GetProjectionAsync(CancellationToken cancellationToken = default);
}
