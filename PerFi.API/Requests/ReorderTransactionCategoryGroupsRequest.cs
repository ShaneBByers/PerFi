namespace PerFi.API.Requests;

public sealed record ReorderTransactionCategoryGroupsRequest(
    IReadOnlyList<int> OrderedTransactionCategoryGroupIds);