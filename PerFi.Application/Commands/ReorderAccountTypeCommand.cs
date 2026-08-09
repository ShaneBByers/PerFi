namespace PerFi.Application.Commands;

public record ReorderAccountTypeCommand(IReadOnlyList<int> OrderedAccountTypeIds);