using PerFi.Domain.Entities;

namespace PerFi.API.Responses;

public sealed record AccountContributionPlanResponse(
    int Id,
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
