using PerFi.Domain.Entities;

namespace PerFi.API.Responses;

public sealed record ContributionResponse(
    int Id,
    DateOnly Date,
    decimal Amount,
    ContributionContributorType Contributor,
    AccountIdentityResponse Account);