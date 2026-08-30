namespace PerFi.Application.Commands;

public sealed record CreateContributionCommand(
    DateOnly Date,
    decimal Amount,
    int ContributionContributorId,
    int AccountId);