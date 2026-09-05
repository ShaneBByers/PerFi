namespace PerFi.Domain.Entities;

public sealed record AccountContributionPlan
{
    public int Id { get; set; }
    public int AccountId { get; }
    public ContributionContributorType ContributorType { get; }
    public decimal DollarAmountPerPayCycle { get; }
    public decimal DollarAmountPerPayCycleAnnualIncrease { get; }
    public decimal DollarAmountAnnual { get; }
    public decimal DollarAmountAnnualIncrease { get; }
    public decimal PercentagePerPayCycle { get; }
    public decimal PercentagePerPayCycleAnnualIncrease { get; }
    public decimal PercentageAnnual { get; }
    public decimal PercentageAnnualIncrease { get; }

    public AccountContributionPlan(
        int accountId,
        ContributionContributorType contributorType,
        decimal dollarAmountPerPayCycle,
        decimal dollarAmountPerPayCycleAnnualIncrease,
        decimal dollarAmountAnnual,
        decimal dollarAmountAnnualIncrease,
        decimal percentagePerPayCycle,
        decimal percentagePerPayCycleAnnualIncrease,
        decimal percentageAnnual,
        decimal percentageAnnualIncrease)
    {
        if (accountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(accountId), "Account ID must be greater than zero.");

        if (!Enum.IsDefined(contributorType))
            throw new ArgumentOutOfRangeException(nameof(contributorType), "Contributor type must be a valid contribution contributor type.");

        AccountId = accountId;
        ContributorType = contributorType;
        DollarAmountPerPayCycle = dollarAmountPerPayCycle;
        DollarAmountPerPayCycleAnnualIncrease = dollarAmountPerPayCycleAnnualIncrease;
        DollarAmountAnnual = dollarAmountAnnual;
        DollarAmountAnnualIncrease = dollarAmountAnnualIncrease;
        PercentagePerPayCycle = percentagePerPayCycle;
        PercentagePerPayCycleAnnualIncrease = percentagePerPayCycleAnnualIncrease;
        PercentageAnnual = percentageAnnual;
        PercentageAnnualIncrease = percentageAnnualIncrease;
    }

    public AccountContributionPlan(
        int id,
        int accountId,
        ContributionContributorType contributorType,
        decimal dollarAmountPerPayCycle,
        decimal dollarAmountPerPayCycleAnnualIncrease,
        decimal dollarAmountAnnual,
        decimal dollarAmountAnnualIncrease,
        decimal percentagePerPayCycle,
        decimal percentagePerPayCycleAnnualIncrease,
        decimal percentageAnnual,
        decimal percentageAnnualIncrease)
        : this(
            accountId,
            contributorType,
            dollarAmountPerPayCycle,
            dollarAmountPerPayCycleAnnualIncrease,
            dollarAmountAnnual,
            dollarAmountAnnualIncrease,
            percentagePerPayCycle,
            percentagePerPayCycleAnnualIncrease,
            percentageAnnual,
            percentageAnnualIncrease)
    {
        Id = id;
    }
}
