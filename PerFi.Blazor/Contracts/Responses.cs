namespace PerFi.Blazor.Contracts;

public sealed record LoginResponse(string Token);

public sealed record AccountTypeGroupResponse(int Id, string Name, int DisplayOrder);

public sealed record AccountTypeResponse(int Id, string Name, int DisplayOrder, AccountTypeGroupResponse Group);

public sealed record InstitutionIdentityResponse(int Id, string Name);

public sealed record AccountResponse(int Id, string Name, int DisplayOrder, InstitutionIdentityResponse Institution, AccountTypeResponse Type);

public sealed record InstitutionResponse(int Id, string Name, int DisplayOrder, IReadOnlyList<AccountResponse> Accounts);

public sealed record AccountBalanceResponse(int SnapshotId, AccountResponse Account, decimal Balance);

public sealed record FinanceSnapshotResponse(int Id, DateOnly Date, IReadOnlyList<AccountBalanceResponse> AccountBalances);
