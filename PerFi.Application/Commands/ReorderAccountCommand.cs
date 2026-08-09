namespace PerFi.Application.Commands;

public record ReorderAccountCommand(IReadOnlyList<int> OrderedAccountIds);