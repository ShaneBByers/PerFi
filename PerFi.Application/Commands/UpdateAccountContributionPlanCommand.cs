using PerFi.Domain.Entities;

namespace PerFi.Application.Commands;

public record UpdateAccountContributionPlanCommand(
    int AccountContributionPlanId,
    int AccountId,
    ContributionContributorType ContributorType,
    DateOnly EffectiveDate,
    decimal DollarAmountPerPayCycle,
    decimal DollarAmountPerPayCycleAnnualIncrease,
    decimal DollarAmountAnnual,
    decimal DollarAmountAnnualIncrease,
    decimal PercentagePerPayCycle,
    decimal PercentagePerPayCycleAnnualIncrease,
    decimal PercentageAnnual,
    decimal PercentageAnnualIncrease);
