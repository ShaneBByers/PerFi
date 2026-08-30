using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IContributionRepository
{
    Task<IReadOnlyList<Contribution>> GetAllContributionsAsync(CancellationToken cancellationToken = default);
    Task<Contribution?> GetContributionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddContributionAsync(Contribution contribution, CancellationToken cancellationToken = default);
    Task<Result> UpdateContributionAsync(Contribution contribution, CancellationToken cancellationToken = default);
    Task<Result> DeleteContributionAsync(int contributionId, CancellationToken cancellationToken = default);
}