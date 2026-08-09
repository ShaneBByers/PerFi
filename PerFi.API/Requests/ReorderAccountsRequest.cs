namespace PerFi.API.Requests;

public sealed record ReorderAccountsRequest(IReadOnlyList<int> OrderedAccountIds);