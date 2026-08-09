namespace PerFi.Blazor.Contracts;

public sealed record LoginRequest(string Username, string Password);

public sealed record CreateAccountTypeRequest(string Name);
public sealed record UpdateAccountTypeRequest(string Name);

public sealed record CreateInstitutionRequest(string InstitutionName);
public sealed record UpdateInstitutionRequest(string InstitutionName);

public sealed record CreateAccountRequest(string AccountName, int InstitutionId, int AccountTypeId);
public sealed record UpdateAccountRequest(string AccountName, int InstitutionId, int AccountTypeId);

public sealed record CreateFinanceSnapshotRequest(DateOnly SnapshotDate, IReadOnlyDictionary<int, decimal> AccountIdToBalanceMap);
public sealed record UpdateFinanceSnapshotRequest(DateOnly SnapshotDate, IReadOnlyDictionary<int, decimal> AccountIdToBalanceMap);
