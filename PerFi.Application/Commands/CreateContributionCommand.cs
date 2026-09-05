using PerFi.Domain.Entities;

namespace PerFi.Application.Commands;

public sealed record CreateContributionCommand(
    DateOnly Date,
    decimal Amount,
    ContributionContributorType Contributor,
    int AccountId);