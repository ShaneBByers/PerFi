namespace PerFi.Application.Commands;

public sealed record UpdateContributionCommand(
    int ContributionId,
    DateOnly Date,
    decimal Amount,
    int ContributionContributorId,
    int AccountId);