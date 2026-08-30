using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class ContributionContributorService(
    IContributionContributorRepository contributionContributorRepository)
    : IContributionContributorService
{
    public async Task<IReadOnlyList<ContributionContributor>> GetAllContributionContributorsAsync(CancellationToken cancellationToken = default)
        => await contributionContributorRepository.GetAllContributionContributorsAsync(cancellationToken);

    public async Task<ContributionContributor?> GetContributionContributorByIdAsync(int id, CancellationToken cancellationToken = default)
        => await contributionContributorRepository.GetContributionContributorByIdAsync(id, cancellationToken);

    public async Task<Result<ContributionContributor>> CreateContributionContributorAsync(CreateContributionContributorCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<ContributionContributor>.Failure("Create contribution contributor command cannot be null.");

        try
        {
            var contributor = new ContributionContributor(command.Name);
            var result = await contributionContributorRepository.AddContributionContributorAsync(contributor, cancellationToken);

            if (!result.IsSuccess)
                return Result<ContributionContributor>.Failure(result.Error);

            contributor.Id = result.Value;
            return Result<ContributionContributor>.Success(contributor);
        }
        catch (ArgumentException ex)
        {
            return Result<ContributionContributor>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateContributionContributorAsync(UpdateContributionContributorCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Update contribution contributor command cannot be null.");

        try
        {
            var contributor = new ContributionContributor(command.ContributionContributorId, command.Name);
            return await contributionContributorRepository.UpdateContributionContributorAsync(contributor, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteContributionContributorAsync(int contributionContributorId, CancellationToken cancellationToken = default)
        => await contributionContributorRepository.DeleteContributionContributorAsync(contributionContributorId, cancellationToken);

    public async Task<Result> ReorderContributionContributorsAsync(ReorderContributionContributorsCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Reorder contribution contributors command cannot be null.");

        return await contributionContributorRepository.ReorderContributionContributorsAsync(command.OrderedContributionContributorIds, cancellationToken);
    }
}