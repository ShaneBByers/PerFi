using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class ContributionRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : IContributionRepository
{
    public async Task<IReadOnlyList<Contribution>> GetAllContributionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Contributions
            .AsNoTracking()
            .Where(contribution => contribution.UserId == currentUserService.UserId)
            .OrderBy(contribution => contribution.Date)
            .ThenBy(contribution => contribution.Id)
            .Include(contribution => contribution.Contributor)
            .Select(contribution => new Contribution(
                contribution.Id,
                contribution.Date,
                contribution.Amount,
                new ContributionContributor(contribution.Contributor.Id, contribution.Contributor.Name)
                {
                    DisplayOrder = contribution.Contributor.DisplayOrder
                },
                contribution.AccountId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Contribution?> GetContributionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var contributionEntity = await dbContext.Contributions
            .AsNoTracking()
            .Include(contribution => contribution.Contributor)
            .FirstOrDefaultAsync(
                contribution => contribution.Id == id && contribution.UserId == currentUserService.UserId,
                cancellationToken);

        return contributionEntity is null
            ? null
            : new Contribution(
                contributionEntity.Id,
                contributionEntity.Date,
                contributionEntity.Amount,
                new ContributionContributor(contributionEntity.Contributor.Id, contributionEntity.Contributor.Name)
                {
                    DisplayOrder = contributionEntity.Contributor.DisplayOrder
                },
                contributionEntity.AccountId);
    }

    public async Task<Result<int>> AddContributionAsync(Contribution contribution, CancellationToken cancellationToken = default)
    {
        var contributor = await dbContext.ContributionContributors
            .FirstOrDefaultAsync(
                contributorEntity => contributorEntity.Id == contribution.Contributor.Id && contributorEntity.UserId == currentUserService.UserId,
                cancellationToken);

        if (contributor is null)
            return Result<int>.Failure($"Contribution contributor with ID '{contribution.Contributor.Id}' does not exist.");

        var account = await dbContext.Accounts
            .FirstOrDefaultAsync(
                accountEntity => accountEntity.Id == contribution.AccountId && accountEntity.Institution.UserId == currentUserService.UserId,
                cancellationToken);

        if (account is null)
            return Result<int>.Failure($"Account with ID '{contribution.AccountId}' does not exist.");

        var entity = new ContributionEntity
        {
            Date = contribution.Date,
            Amount = contribution.Amount,
            UserId = currentUserService.UserId,
            ContributorId = contributor.Id,
            Contributor = contributor,
            AccountId = account.Id,
            Account = account
        };

        dbContext.Contributions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateContributionAsync(Contribution contribution, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Contributions
            .FirstOrDefaultAsync(
                contributionEntity => contributionEntity.Id == contribution.Id && contributionEntity.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"Contribution with ID '{contribution.Id}' not found.");

        var contributor = await dbContext.ContributionContributors
            .FirstOrDefaultAsync(
                contributorEntity => contributorEntity.Id == contribution.Contributor.Id && contributorEntity.UserId == currentUserService.UserId,
                cancellationToken);

        if (contributor is null)
            return Result.Failure($"Contribution contributor with ID '{contribution.Contributor.Id}' does not exist.");

        var account = await dbContext.Accounts
            .FirstOrDefaultAsync(
                accountEntity => accountEntity.Id == contribution.AccountId && accountEntity.Institution.UserId == currentUserService.UserId,
                cancellationToken);

        if (account is null)
            return Result.Failure($"Account with ID '{contribution.AccountId}' does not exist.");

        entity.Date = contribution.Date;
        entity.Amount = contribution.Amount;
        entity.ContributorId = contributor.Id;
        entity.AccountId = account.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteContributionAsync(int contributionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Contributions
            .FirstOrDefaultAsync(
                contribution => contribution.Id == contributionId && contribution.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"Contribution with ID '{contributionId}' not found.");

        dbContext.Contributions.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
