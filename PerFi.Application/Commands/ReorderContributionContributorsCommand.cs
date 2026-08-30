namespace PerFi.Application.Commands;

public sealed record ReorderContributionContributorsCommand(
    IReadOnlyList<int> OrderedContributionContributorIds);