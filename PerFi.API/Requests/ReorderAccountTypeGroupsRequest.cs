namespace PerFi.API.Requests;

public sealed record ReorderAccountTypeGroupsRequest(IReadOnlyList<int> OrderedAccountTypeGroupIds);