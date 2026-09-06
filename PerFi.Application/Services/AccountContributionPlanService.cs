using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class AccountContributionPlanService(IAccountContributionPlanRepository accountContributionPlanRepository)
    : IAccountContributionPlanService
{
    public async Task<IReadOnlyList<AccountContributionPlan>> GetAllByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
        => await accountContributionPlanRepository.GetAllByAccountIdAsync(accountId, cancellationToken);

    public async Task<AccountContributionPlan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await accountContributionPlanRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Result<AccountContributionPlan>> CreateAsync(CreateAccountContributionPlanCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<AccountContributionPlan>.Failure("Create account contribution plan command cannot be null.");

        try
        {
            var plan = new AccountContributionPlan(
                command.AccountId,
                command.ContributorType,
                command.EffectiveDate,
                command.DollarAmountPerPayCycle,
                command.DollarAmountPerPayCycleAnnualIncrease,
                command.DollarAmountAnnual,
                command.DollarAmountAnnualIncrease,
                command.PercentagePerPayCycle,
                command.PercentagePerPayCycleAnnualIncrease,
                command.PercentageAnnual,
                command.PercentageAnnualIncrease);

            var result = await accountContributionPlanRepository.AddAsync(plan, cancellationToken);

            if (!result.IsSuccess)
                return Result<AccountContributionPlan>.Failure(result.Error);

            plan.Id = result.Value;
            return Result<AccountContributionPlan>.Success(plan);
        }
        catch (ArgumentException ex)
        {
            return Result<AccountContributionPlan>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateAsync(UpdateAccountContributionPlanCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Update account contribution plan command cannot be null.");

        try
        {
            var plan = new AccountContributionPlan(
                command.AccountContributionPlanId,
                command.AccountId,
                command.ContributorType,
                command.EffectiveDate,
                command.DollarAmountPerPayCycle,
                command.DollarAmountPerPayCycleAnnualIncrease,
                command.DollarAmountAnnual,
                command.DollarAmountAnnualIncrease,
                command.PercentagePerPayCycle,
                command.PercentagePerPayCycleAnnualIncrease,
                command.PercentageAnnual,
                command.PercentageAnnualIncrease);

            return await accountContributionPlanRepository.UpdateAsync(plan, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(int accountContributionPlanId, CancellationToken cancellationToken = default)
        => await accountContributionPlanRepository.DeleteAsync(accountContributionPlanId, cancellationToken);
}
