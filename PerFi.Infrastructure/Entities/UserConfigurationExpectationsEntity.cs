namespace PerFi.Infrastructure.Entities;

public class UserConfigurationExpectationsEntity
{
    public int Id { get; set; }
    public decimal AnnualSalaryRaisePercentage { get; set; }
    public decimal EmployerMatchPercentage { get; set; }
    public decimal EmployerProfitSharingPercentage { get; set; }
    public decimal AnnualBrokerageContributionPerPayCycleIncrease { get; set; }
    public decimal AnnualStockMarketReturnPercentage { get; set; }
    public decimal AnnualInflationPercentage { get; set; }
    public DateTimeOffset LastVerifiedDateTime { get; set; }
    public required string UserId { get; set; }
}
