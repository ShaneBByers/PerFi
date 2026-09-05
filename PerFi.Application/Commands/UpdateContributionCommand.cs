using PerFi.Domain.Entities;

namespace PerFi.Application.Commands;

public sealed record UpdateContributionCommand(
    int ContributionId,
    DateOnly Date,
    decimal Amount,
    ContributionContributorType Contributor,
    int AccountId);