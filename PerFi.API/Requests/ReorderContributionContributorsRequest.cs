namespace PerFi.API.Requests;

public sealed record ReorderContributionContributorsRequest(
    IReadOnlyList<int> OrderedContributionContributorIds);