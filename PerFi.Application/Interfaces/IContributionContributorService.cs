using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface IContributionContributorService
{
    Task<IReadOnlyList<ContributionContributor>> GetAllContributionContributorsAsync(CancellationToken cancellationToken = default);
    Task<ContributionContributor?> GetContributionContributorByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ContributionContributor>> CreateContributionContributorAsync(CreateContributionContributorCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateContributionContributorAsync(UpdateContributionContributorCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteContributionContributorAsync(int contributionContributorId, CancellationToken cancellationToken = default);
    Task<Result> ReorderContributionContributorsAsync(ReorderContributionContributorsCommand command, CancellationToken cancellationToken = default);
}