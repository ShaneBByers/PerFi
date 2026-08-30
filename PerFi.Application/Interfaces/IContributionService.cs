using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface IContributionService
{
    Task<IReadOnlyList<Contribution>> GetAllContributionsAsync(CancellationToken cancellationToken = default);
    Task<Contribution?> GetContributionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Contribution>> CreateContributionAsync(CreateContributionCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateContributionAsync(UpdateContributionCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteContributionAsync(int contributionId, CancellationToken cancellationToken = default);
}