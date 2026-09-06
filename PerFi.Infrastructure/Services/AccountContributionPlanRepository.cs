using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class AccountContributionPlanRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : IAccountContributionPlanRepository
{
    public async Task<IReadOnlyList<AccountContributionPlan>> GetAllByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await dbContext.AccountContributionPlans
            .AsNoTracking()
            .Where(plan => plan.AccountId == accountId && plan.Account.UserId == currentUserService.UserId)
            .OrderBy(plan => plan.ContributorType)
            .ThenBy(plan => plan.EffectiveDate)
            .ThenBy(plan => plan.Id)
            .Select(plan => new AccountContributionPlan(
                plan.Id,
                plan.AccountId,
                plan.ContributorType,
                plan.EffectiveDate,
                plan.DollarAmountPerPayCycle,
                plan.DollarAmountPerPayCycleAnnualIncrease,
                plan.DollarAmountAnnual,
                plan.DollarAmountAnnualIncrease,
                plan.PercentagePerPayCycle,
                plan.PercentagePerPayCycleAnnualIncrease,
                plan.PercentageAnnual,
                plan.PercentageAnnualIncrease))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountContributionPlan>> GetAllForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AccountContributionPlans
            .AsNoTracking()
            .Where(plan => plan.Account.UserId == currentUserService.UserId)
            .OrderBy(plan => plan.AccountId)
            .ThenBy(plan => plan.ContributorType)
            .ThenBy(plan => plan.EffectiveDate)
            .Select(plan => new AccountContributionPlan(
                plan.Id,
                plan.AccountId,
                plan.ContributorType,
                plan.EffectiveDate,
                plan.DollarAmountPerPayCycle,
                plan.DollarAmountPerPayCycleAnnualIncrease,
                plan.DollarAmountAnnual,
                plan.DollarAmountAnnualIncrease,
                plan.PercentagePerPayCycle,
                plan.PercentagePerPayCycleAnnualIncrease,
                plan.PercentageAnnual,
                plan.PercentageAnnualIncrease))
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountContributionPlan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AccountContributionPlans
            .AsNoTracking()
            .Include(plan => plan.Account)
            .FirstOrDefaultAsync(
                plan => plan.Id == id && plan.Account.UserId == currentUserService.UserId,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<Result<int>> AddAsync(AccountContributionPlan plan, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.Accounts
            .FirstOrDefaultAsync(
                accountEntity => accountEntity.Id == plan.AccountId && accountEntity.UserId == currentUserService.UserId,
                cancellationToken);

        if (account is null)
            return Result<int>.Failure($"Account with ID '{plan.AccountId}' does not exist.");

        if (await dbContext.AccountContributionPlans.AnyAsync(
                existing => existing.AccountId == plan.AccountId
                            && existing.ContributorType == plan.ContributorType
                            && existing.EffectiveDate == plan.EffectiveDate,
                cancellationToken))
        {
            return Result<int>.Failure($"A contribution plan for contributor type '{plan.ContributorType}' effective '{plan.EffectiveDate}' already exists for this account.");
        }

        var entity = new AccountContributionPlanEntity
        {
            AccountId = account.Id,
            Account = account,
            ContributorType = plan.ContributorType,
            EffectiveDate = plan.EffectiveDate,
            DollarAmountPerPayCycle = plan.DollarAmountPerPayCycle,
            DollarAmountPerPayCycleAnnualIncrease = plan.DollarAmountPerPayCycleAnnualIncrease,
            DollarAmountAnnual = plan.DollarAmountAnnual,
            DollarAmountAnnualIncrease = plan.DollarAmountAnnualIncrease,
            PercentagePerPayCycle = plan.PercentagePerPayCycle,
            PercentagePerPayCycleAnnualIncrease = plan.PercentagePerPayCycleAnnualIncrease,
            PercentageAnnual = plan.PercentageAnnual,
            PercentageAnnualIncrease = plan.PercentageAnnualIncrease
        };

        dbContext.AccountContributionPlans.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateAsync(AccountContributionPlan plan, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AccountContributionPlans
            .FirstOrDefaultAsync(
                existing => existing.Id == plan.Id && existing.Account.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"Account contribution plan with ID '{plan.Id}' not found.");

        if (await dbContext.AccountContributionPlans.AnyAsync(
                existing => existing.AccountId == plan.AccountId
                            && existing.ContributorType == plan.ContributorType
                            && existing.EffectiveDate == plan.EffectiveDate
                            && existing.Id != plan.Id,
                cancellationToken))
        {
            return Result.Failure($"A contribution plan for contributor type '{plan.ContributorType}' effective '{plan.EffectiveDate}' already exists for this account.");
        }

        entity.ContributorType = plan.ContributorType;
        entity.EffectiveDate = plan.EffectiveDate;
        entity.DollarAmountPerPayCycle = plan.DollarAmountPerPayCycle;
        entity.DollarAmountPerPayCycleAnnualIncrease = plan.DollarAmountPerPayCycleAnnualIncrease;
        entity.DollarAmountAnnual = plan.DollarAmountAnnual;
        entity.DollarAmountAnnualIncrease = plan.DollarAmountAnnualIncrease;
        entity.PercentagePerPayCycle = plan.PercentagePerPayCycle;
        entity.PercentagePerPayCycleAnnualIncrease = plan.PercentagePerPayCycleAnnualIncrease;
        entity.PercentageAnnual = plan.PercentageAnnual;
        entity.PercentageAnnualIncrease = plan.PercentageAnnualIncrease;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AccountContributionPlans
            .FirstOrDefaultAsync(
                plan => plan.Id == id && plan.Account.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"Account contribution plan with ID '{id}' not found.");

        dbContext.AccountContributionPlans.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static AccountContributionPlan ToDomain(AccountContributionPlanEntity entity)
    {
        return new AccountContributionPlan(
            entity.Id,
            entity.AccountId,
            entity.ContributorType,
            entity.EffectiveDate,
            entity.DollarAmountPerPayCycle,
            entity.DollarAmountPerPayCycleAnnualIncrease,
            entity.DollarAmountAnnual,
            entity.DollarAmountAnnualIncrease,
            entity.PercentagePerPayCycle,
            entity.PercentagePerPayCycleAnnualIncrease,
            entity.PercentageAnnual,
            entity.PercentageAnnualIncrease);
    }
}
