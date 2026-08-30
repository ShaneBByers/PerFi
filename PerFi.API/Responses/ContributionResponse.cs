namespace PerFi.API.Responses;

public sealed record ContributionResponse(
    int Id,
    DateOnly Date,
    decimal Amount,
    ContributionContributorIdentityResponse Contributor,
    AccountIdentityResponse Account);