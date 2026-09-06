namespace PerFi.Blazor.Contracts;

public sealed record LoginRequest(string Username, string Password);

public sealed record CreateAccountTypeGroupRequest(string Name);
public sealed record UpdateAccountTypeGroupRequest(string Name);
public sealed record ReorderAccountTypeGroupsRequest(IReadOnlyList<int> OrderedAccountTypeGroupIds);

public sealed record CreateAccountTypeRequest(string Name, int AccountTypeGroupId);
public sealed record UpdateAccountTypeRequest(string Name, int AccountTypeGroupId);
public sealed record ReorderAccountTypesRequest(IReadOnlyList<int> OrderedAccountTypeIds);

public sealed record CreateInstitutionRequest(string InstitutionName);
public sealed record UpdateInstitutionRequest(string InstitutionName);
public sealed record ReorderInstitutionsRequest(IReadOnlyList<int> OrderedInstitutionIds);

public sealed record CreateAccountRequest(string AccountName, int InstitutionId, int AccountTypeId, decimal ExpectedAnnualGrowthPercentage);
public sealed record UpdateAccountRequest(string AccountName, int InstitutionId, int AccountTypeId, decimal ExpectedAnnualGrowthPercentage);
public sealed record ReorderAccountsRequest(IReadOnlyList<int> OrderedAccountIds);

public sealed record CreateFinanceSnapshotRequest(DateOnly SnapshotDate, IReadOnlyDictionary<int, decimal> AccountIdToBalanceMap);
public sealed record UpdateFinanceSnapshotRequest(DateOnly SnapshotDate, IReadOnlyDictionary<int, decimal> AccountIdToBalanceMap);
public sealed record BulkUpdateFinanceSnapshotCellsRequest(IReadOnlyList<SnapshotCellUpdateRequest> Updates);
public sealed record SnapshotCellUpdateRequest(int SnapshotId, int AccountId, decimal Balance);

public sealed record CreateContributionRequest(DateOnly Date, decimal Amount, ContributionContributorType Contributor, int AccountId);
public sealed record UpdateContributionRequest(DateOnly Date, decimal Amount, ContributionContributorType Contributor, int AccountId);

public sealed record CreateSalaryProgressionRequest(DateOnly EffectiveDate, decimal AnnualSalary);
public sealed record UpdateSalaryProgressionRequest(DateOnly EffectiveDate, decimal AnnualSalary);

public sealed record CreateUserConfigurationRequest(
    DateOnly BirthDate,
    PayCycleType PayCycleType,
    DateOnly ReferencePayDate,
    decimal ExpectedAnnualSalaryRaisePercentage,
    decimal ExpectedAnnualInflationPercentage);

public sealed record UpdateUserConfigurationRequest(
    DateOnly BirthDate,
    PayCycleType PayCycleType,
    DateOnly ReferencePayDate,
    decimal ExpectedAnnualSalaryRaisePercentage,
    decimal ExpectedAnnualInflationPercentage);

public sealed record CreateAccountContributionPlanRequest(
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

public sealed record UpdateAccountContributionPlanRequest(
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

public sealed record CreateTransactionCategoryGroupRequest(string Name);
public sealed record UpdateTransactionCategoryGroupRequest(string Name);
public sealed record ReorderTransactionCategoryGroupsRequest(IReadOnlyList<int> OrderedTransactionCategoryGroupIds);

public sealed record CreateTransactionCategoryRequest(string Name, int TransactionCategoryGroupId);
public sealed record UpdateTransactionCategoryRequest(string Name, int TransactionCategoryGroupId);
public sealed record ReorderTransactionCategoriesRequest(IReadOnlyList<int> OrderedTransactionCategoryIds);

public sealed record CreateTransactionRequest(DateOnly Date, string CounterpartyName, decimal Amount, int TransactionCategoryId, int AccountId, string? Description);
public sealed record UpdateTransactionRequest(DateOnly Date, string CounterpartyName, decimal Amount, int TransactionCategoryId, int AccountId, string? Description);
