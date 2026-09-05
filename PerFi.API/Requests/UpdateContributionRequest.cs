using PerFi.Domain.Entities;

namespace PerFi.API.Requests;

public sealed record UpdateContributionRequest(
    DateOnly Date,
    decimal Amount,
    ContributionContributorType Contributor,
    int AccountId);