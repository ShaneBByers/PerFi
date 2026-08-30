namespace PerFi.API.Requests;

public sealed record CreateContributionRequest(
    DateOnly Date,
    decimal Amount,
    int ContributionContributorId,
    int AccountId);