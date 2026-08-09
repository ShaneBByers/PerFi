namespace PerFi.Application.Commands;

public record ReorderInstitutionCommand(IReadOnlyList<int> OrderedInstitutionIds);