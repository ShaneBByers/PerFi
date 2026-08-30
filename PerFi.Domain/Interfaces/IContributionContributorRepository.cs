using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IContributionContributorRepository
{
    Task<IReadOnlyList<ContributionContributor>> GetAllContributionContributorsAsync(CancellationToken cancellationToken = default);
    Task<ContributionContributor?> GetContributionContributorByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddContributionContributorAsync(ContributionContributor contributionContributor, CancellationToken cancellationToken = default);
    Task<Result> UpdateContributionContributorAsync(ContributionContributor contributionContributor, CancellationToken cancellationToken = default);
    Task<Result> DeleteContributionContributorAsync(int contributionContributorId, CancellationToken cancellationToken = default);
    Task<Result> ReorderContributionContributorsAsync(IReadOnlyList<int> orderedContributionContributorIds, CancellationToken cancellationToken = default);
}
