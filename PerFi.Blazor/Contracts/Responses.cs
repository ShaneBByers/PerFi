namespace PerFi.Blazor.Contracts;

public sealed record LoginResponse(string Token);

public sealed record AccountTypeResponse(int Id, string Name);

public sealed record AccountResponse(int Id, string Name, AccountTypeResponse Type);

public sealed record InstitutionResponse(int Id, string Name, IReadOnlyList<AccountResponse> Accounts);

public sealed record AccountBalanceResponse(AccountResponse Account, decimal Balance);

public sealed record FinanceSnapshotResponse(DateOnly Date, IReadOnlyList<AccountBalanceResponse> AccountBalances);
