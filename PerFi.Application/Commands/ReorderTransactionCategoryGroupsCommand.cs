namespace PerFi.Application.Commands;

public sealed record ReorderTransactionCategoryGroupsCommand(
    IReadOnlyList<int> OrderedTransactionCategoryGroupIds);