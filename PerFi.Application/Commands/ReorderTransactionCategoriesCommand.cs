namespace PerFi.Application.Commands;

public sealed record ReorderTransactionCategoriesCommand(
    IReadOnlyList<int> OrderedTransactionCategoryIds);