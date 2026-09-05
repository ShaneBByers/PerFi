using PerFi.Domain.Entities;

namespace PerFi.API.Requests;

public sealed record CreateContributionRequest(
    DateOnly Date,
    decimal Amount,
    ContributionContributorType Contributor,
    int AccountId);