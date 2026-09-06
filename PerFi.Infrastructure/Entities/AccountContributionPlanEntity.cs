using PerFi.Domain.Entities;

namespace PerFi.Infrastructure.Entities;

public class AccountContributionPlanEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public AccountEntity Account { get; set; } = null!;
    public ContributionContributorType ContributorType { get; set; }
    public DateOnly EffectiveDate { get; set; }

    public decimal DollarAmountPerPayCycle { get; set; }
    public decimal DollarAmountPerPayCycleAnnualIncrease { get; set; }
    public decimal DollarAmountAnnual { get; set; }
    public decimal DollarAmountAnnualIncrease { get; set; }
    public decimal PercentagePerPayCycle { get; set; }
    public decimal PercentagePerPayCycleAnnualIncrease { get; set; }
    public decimal PercentageAnnual { get; set; }
    public decimal PercentageAnnualIncrease { get; set; }
}
