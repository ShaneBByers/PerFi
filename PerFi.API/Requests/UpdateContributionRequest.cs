namespace PerFi.API.Requests;

public sealed record UpdateContributionRequest(
    DateOnly Date,
    decimal Amount,
    int ContributionContributorId,
    int AccountId);