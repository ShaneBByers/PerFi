namespace PerFi.API.Requests;

public sealed record ReorderInstitutionsRequest(IReadOnlyList<int> OrderedInstitutionIds);