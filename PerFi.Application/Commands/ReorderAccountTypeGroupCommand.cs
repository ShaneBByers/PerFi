namespace PerFi.Application.Commands;

public record ReorderAccountTypeGroupCommand(IReadOnlyList<int> OrderedAccountTypeGroupIds);