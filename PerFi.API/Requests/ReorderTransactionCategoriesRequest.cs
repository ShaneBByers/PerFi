namespace PerFi.API.Requests;

public sealed record ReorderTransactionCategoriesRequest(
    IReadOnlyList<int> OrderedTransactionCategoryIds);