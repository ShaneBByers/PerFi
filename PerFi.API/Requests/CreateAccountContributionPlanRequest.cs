using PerFi.Domain.Entities;

namespace PerFi.API.Requests;

public record CreateAccountContributionPlanRequest(
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
