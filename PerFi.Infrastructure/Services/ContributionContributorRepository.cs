using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class ContributionContributorRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : IContributionContributorRepository
{
    public async Task<IReadOnlyList<ContributionContributor>> GetAllContributionContributorsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ContributionContributors
            .AsNoTracking()
            .Where(contributor => contributor.UserId == currentUserService.UserId)
            .OrderBy(contributor => contributor.DisplayOrder)
            .ThenBy(contributor => contributor.Name)
            .ThenBy(contributor => contributor.Id)
            .Select(contributor => new ContributionContributor(contributor.Id, contributor.Name)
            {
                DisplayOrder = contributor.DisplayOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ContributionContributor?> GetContributionContributorByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var contributorEntity = await dbContext.ContributionContributors
            .AsNoTracking()
            .FirstOrDefaultAsync(
                contributor => contributor.Id == id && contributor.UserId == currentUserService.UserId,
                cancellationToken);

        return contributorEntity is null
            ? null
            : new ContributionContributor(contributorEntity.Id, contributorEntity.Name)
            {
                DisplayOrder = contributorEntity.DisplayOrder
            };
    }

    public async Task<Result<int>> AddContributionContributorAsync(ContributionContributor contributionContributor, CancellationToken cancellationToken = default)
    {
        if (await dbContext.ContributionContributors.AnyAsync(
                contributor => contributor.Name == contributionContributor.Name && contributor.UserId == currentUserService.UserId,
                cancellationToken))
        {
            return Result<int>.Failure($"A contribution contributor with name '{contributionContributor.Name}' already exists.");
        }

        var nextDisplayOrder = await dbContext.ContributionContributors
            .Where(contributor => contributor.UserId == currentUserService.UserId)
            .Select(contributor => (int?)contributor.DisplayOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var contributorEntity = new ContributionContributorEntity
        {
            Name = contributionContributor.Name,
            DisplayOrder = nextDisplayOrder + 1,
            UserId = currentUserService.UserId,
            Contributions = []
        };

        dbContext.ContributionContributors.Add(contributorEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(contributorEntity.Id);
    }

    public async Task<Result> UpdateContributionContributorAsync(ContributionContributor contributionContributor, CancellationToken cancellationToken = default)
    {
        var contributorEntity = await dbContext.ContributionContributors
            .FirstOrDefaultAsync(
                contributor => contributor.Id == contributionContributor.Id && contributor.UserId == currentUserService.UserId,
                cancellationToken);

        if (contributorEntity is null)
            return Result.Failure($"Contribution contributor with ID '{contributionContributor.Id}' not found.");

        var hasDuplicateName = await dbContext.ContributionContributors
            .AnyAsync(
                contributor => contributor.Id != contributionContributor.Id
                               && contributor.Name == contributionContributor.Name
                               && contributor.UserId == currentUserService.UserId,
                cancellationToken);

        if (hasDuplicateName)
            return Result.Failure($"A contribution contributor with name '{contributionContributor.Name}' already exists.");

        contributorEntity.Name = contributionContributor.Name;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteContributionContributorAsync(int contributionContributorId, CancellationToken cancellationToken = default)
    {
        var contributorEntity = await dbContext.ContributionContributors
            .FirstOrDefaultAsync(
                contributor => contributor.Id == contributionContributorId && contributor.UserId == currentUserService.UserId,
                cancellationToken);

        if (contributorEntity is null)
            return Result.Failure($"Contribution contributor with ID '{contributionContributorId}' not found.");

        var isReferenced = await dbContext.Contributions
            .AnyAsync(contribution => contribution.ContributorId == contributionContributorId, cancellationToken);

        if (isReferenced)
            return Result.Failure("Cannot delete contribution contributor because one or more contributions reference it.");

        dbContext.ContributionContributors.Remove(contributorEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ReorderContributionContributorsAsync(IReadOnlyList<int> orderedContributionContributorIds, CancellationToken cancellationToken = default)
    {
        var normalizedIds = orderedContributionContributorIds.Distinct().ToList();
        if (normalizedIds.Count != orderedContributionContributorIds.Count)
            return Result.Failure("Contribution contributor reorder list contains duplicate IDs.");

        var contributorEntities = await dbContext.ContributionContributors
            .Where(contributor => normalizedIds.Contains(contributor.Id) && contributor.UserId == currentUserService.UserId)
            .ToListAsync(cancellationToken);

        if (contributorEntities.Count != normalizedIds.Count)
            return Result.Failure("One or more contribution contributors in the reorder list do not exist.");

        var entitiesById = contributorEntities.ToDictionary(contributor => contributor.Id);

        for (var index = 0; index < normalizedIds.Count; index++)
        {
            entitiesById[normalizedIds[index]].DisplayOrder = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
