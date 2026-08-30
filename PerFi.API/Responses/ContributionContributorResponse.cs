namespace PerFi.API.Responses;

public sealed record ContributionContributorResponse(
    int Id,
    string Name,
    int DisplayOrder);