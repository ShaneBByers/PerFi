using PerFi.Domain.Entities;

namespace PerFi.Application.Commands;

public record CreateAccountContributionPlanCommand(
    int AccountId,
    ContributionContributorType ContributorType,
    decimal DollarAmountPerPayCycle,
    decimal DollarAmountPerPayCycleAnnualIncrease,
    decimal DollarAmountAnnual,
    decimal DollarAmountAnnualIncrease,
    decimal PercentagePerPayCycle,
    decimal PercentagePerPayCycleAnnualIncrease,
    decimal PercentageAnnual,
    decimal PercentageAnnualIncrease);
