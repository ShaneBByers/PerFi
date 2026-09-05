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
            .Select(contribution => new Contribution(
                contribution.Id,
                contribution.Date,
                contribution.Amount,
                contribution.Contributor,
                contribution.AccountId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Contribution?> GetContributionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var contributionEntity = await dbContext.Contributions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                contribution => contribution.Id == id && contribution.UserId == currentUserService.UserId,
                cancellationToken);

        return contributionEntity is null
            ? null
            : new Contribution(
                contributionEntity.Id,
                contributionEntity.Date,
                contributionEntity.Amount,
                contributionEntity.Contributor,
                contributionEntity.AccountId);
    }

    public async Task<Result<int>> AddContributionAsync(Contribution contribution, CancellationToken cancellationToken = default)
    {
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
            Contributor = contribution.Contributor,
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

        var account = await dbContext.Accounts
            .FirstOrDefaultAsync(
                accountEntity => accountEntity.Id == contribution.AccountId && accountEntity.Institution.UserId == currentUserService.UserId,
                cancellationToken);

        if (account is null)
            return Result.Failure($"Account with ID '{contribution.AccountId}' does not exist.");

        entity.Date = contribution.Date;
        entity.Amount = contribution.Amount;
        entity.Contributor = contribution.Contributor;
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

