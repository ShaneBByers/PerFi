namespace PerFi.Blazor.Contracts;

public sealed record LoginResponse(string Token);

// Mirrors PerFi.Domain.Entities.ContributionContributorType; ordinal values must stay in sync since the wire format is numeric.
public enum ContributionContributorType
{
    Self,
    Employer,
    Other
}

// Mirrors PerFi.Domain.Entities.PayCycleType; ordinal values must stay in sync since the wire format is numeric.
public enum PayCycleType
{
    Weekly,
    BiWeekly,
    SemiMonthly,
    Monthly
}

public sealed record AccountTypeGroupResponse(int Id, string Name, int DisplayOrder);

public sealed record AccountTypeResponse(int Id, string Name, int DisplayOrderInGroup, AccountTypeGroupResponse Group);

public sealed record InstitutionIdentityResponse(int Id, string Name);

public sealed record AccountResponse(int Id, string Name, int DisplayOrderInGroup, InstitutionIdentityResponse Institution, AccountTypeResponse Type, decimal ExpectedAnnualGrowthPercentage);

public sealed record InstitutionResponse(int Id, string Name, int DisplayOrder, IReadOnlyList<AccountResponse> Accounts);

public sealed record AccountBalanceResponse(int SnapshotId, AccountResponse Account, decimal Balance);

public sealed record FinanceSnapshotResponse(int Id, DateOnly Date, IReadOnlyList<AccountBalanceResponse> AccountBalances);

public sealed record AccountIdentityResponse(int Id, string Name);

public sealed record ContributionResponse(int Id, DateOnly Date, decimal Amount, ContributionContributorType Contributor, AccountIdentityResponse Account);

public sealed record SalaryProgressionResponse(int Id, DateOnly EffectiveDate, decimal AnnualSalary);

public sealed record UserConfigurationResponse(
    int Id,
    DateOnly BirthDate,
    PayCycleType PayCycleType,
    DateOnly ReferencePayDate,
    decimal ExpectedAnnualSalaryRaisePercentage,
    decimal ExpectedAnnualInflationPercentage,
    int RetirementAge);

public sealed record AccountContributionPlanResponse(
    int Id,
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

public sealed record TransactionCategoryGroupResponse(int Id, string Name, int DisplayOrder);

public sealed record TransactionCategoryGroupIdentityResponse(int Id, string Name);

public sealed record TransactionCategoryResponse(int Id, string Name, int DisplayOrderInGroup, TransactionCategoryGroupIdentityResponse Group);

public sealed record TransactionCategoryIdentityResponse(int Id, string Name, TransactionCategoryGroupIdentityResponse Group);

public sealed record TransactionResponse(int Id, DateOnly Date, string CounterpartyName, decimal Amount, string? Description, TransactionCategoryIdentityResponse Category, AccountIdentityResponse Account);

// Mirrors PerFi.Domain.Entities.Projections.ProjectionPeriodType; ordinal values must stay in sync since the wire format is numeric.
public enum ProjectionPeriodType
{
    Annual,
    Quarterly
}

public sealed record ProjectionAmountsResponse(
    decimal StartingAmount,
    decimal ContributedAmount,
    decimal GrowthAmount,
    decimal FinalAmount);

public sealed record ProjectionGroupResponse(
    string GroupName,
    ProjectionAmountsResponse Amounts,
    ProjectionAmountsResponse AmountsInTodaysDollars,
    ProjectionAmountsResponse? ExpectedAmounts,
    ProjectionAmountsResponse? ExpectedAmountsInTodaysDollars);

public sealed record ProjectionPeriodResponse(
    ProjectionPeriodType PeriodType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int CalendarYear,
    int? Quarter,
    bool IsProjected,
    int UserAgeAtStart,
    int UserAgeAtEnd,
    decimal FractionalAge,
    decimal SalaryAtStart,
    decimal SalaryAtEnd,
    IReadOnlyList<ProjectionGroupResponse> Groups,
    ProjectionGroupResponse Total);
