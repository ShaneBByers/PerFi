namespace PerFi.Domain.Entities;

public sealed record UserConfigurationExpectations
{
    public int Id { get; set; }
    public decimal AnnualSalaryRaisePercentage { get; }
    public decimal EmployerMatchPercentage { get; }
    public decimal EmployerProfitSharingPercentage { get; }
    public decimal AnnualBrokerageContributionPerPayCycleIncrease { get; }
    public decimal AnnualStockMarketReturnPercentage { get; }
    public decimal AnnualInflationPercentage { get; }
    public DateTimeOffset LastVerifiedDateTime { get; }

    public UserConfigurationExpectations(
        decimal annualSalaryRaisePercentage,
        decimal employerMatchPercentage,
        decimal employerProfitSharingPercentage,
        decimal annualBrokerageContributionPerPayCycleIncrease,
        decimal annualStockMarketReturnPercentage,
        decimal annualInflationPercentage,
        DateTimeOffset lastVerifiedDateTime)
    {
        AnnualSalaryRaisePercentage = annualSalaryRaisePercentage;
        EmployerMatchPercentage = employerMatchPercentage;
        EmployerProfitSharingPercentage = employerProfitSharingPercentage;
        AnnualBrokerageContributionPerPayCycleIncrease = annualBrokerageContributionPerPayCycleIncrease;
        AnnualStockMarketReturnPercentage = annualStockMarketReturnPercentage;
        AnnualInflationPercentage = annualInflationPercentage;
        LastVerifiedDateTime = lastVerifiedDateTime;
    }

    public UserConfigurationExpectations(
        int id,
        decimal annualSalaryRaisePercentage,
        decimal employerMatchPercentage,
        decimal employerProfitSharingPercentage,
        decimal annualBrokerageContributionPerPayCycleIncrease,
        decimal annualStockMarketReturnPercentage,
        decimal annualInflationPercentage,
        DateTimeOffset lastVerifiedDateTime)
        : this(
            annualSalaryRaisePercentage,
            employerMatchPercentage,
            employerProfitSharingPercentage,
            annualBrokerageContributionPerPayCycleIncrease,
            annualStockMarketReturnPercentage,
            annualInflationPercentage,
            lastVerifiedDateTime)
    {
        Id = id;
    }
}