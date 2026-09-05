namespace PerFi.Blazor.Contracts;

public sealed record LoginResponse(string Token);

// Mirrors PerFi.Domain.Entities.ContributionContributorType; ordinal values must stay in sync since the wire format is numeric.
public enum ContributionContributorType
{
    Self,
    Employer,
    Other
}

public sealed record AccountTypeGroupResponse(int Id, string Name, int DisplayOrder);

public sealed record AccountTypeResponse(int Id, string Name, int DisplayOrder, AccountTypeGroupResponse Group);

public sealed record InstitutionIdentityResponse(int Id, string Name);

public sealed record AccountResponse(int Id, string Name, int DisplayOrder, InstitutionIdentityResponse Institution, AccountTypeResponse Type);

public sealed record InstitutionResponse(int Id, string Name, int DisplayOrder, IReadOnlyList<AccountResponse> Accounts);

public sealed record AccountBalanceResponse(int SnapshotId, AccountResponse Account, decimal Balance);

public sealed record FinanceSnapshotResponse(int Id, DateOnly Date, IReadOnlyList<AccountBalanceResponse> AccountBalances);

public sealed record AccountIdentityResponse(int Id, string Name);

public sealed record ContributionResponse(int Id, DateOnly Date, decimal Amount, ContributionContributorType Contributor, AccountIdentityResponse Account);

public sealed record TransactionCategoryGroupResponse(int Id, string Name, int DisplayOrder);

public sealed record TransactionCategoryGroupIdentityResponse(int Id, string Name);

public sealed record TransactionCategoryResponse(int Id, string Name, int DisplayOrder, TransactionCategoryGroupIdentityResponse Group);

public sealed record TransactionCategoryIdentityResponse(int Id, string Name, TransactionCategoryGroupIdentityResponse Group);

public sealed record TransactionResponse(int Id, DateOnly Date, string CounterpartyName, decimal Amount, string? Description, TransactionCategoryIdentityResponse Category, AccountIdentityResponse Account);
