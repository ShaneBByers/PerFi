using PerFi.Domain.Entities;

namespace PerFi.API.Requests;

public record UpdateAccountContributionPlanRequest(
    ContributionContributorType ContributorType,
    decimal DollarAmountPerPayCycle,
    decimal DollarAmountPerPayCycleAnnualIncrease,
    decimal DollarAmountAnnual,
    decimal DollarAmountAnnualIncrease,
    decimal PercentagePerPayCycle,
    decimal PercentagePerPayCycleAnnualIncrease,
    decimal PercentageAnnual,
    decimal PercentageAnnualIncrease);
