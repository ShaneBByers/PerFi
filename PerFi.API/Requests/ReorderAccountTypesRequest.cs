namespace PerFi.API.Requests;

public sealed record ReorderAccountTypesRequest(IReadOnlyList<int> OrderedAccountTypeIds);