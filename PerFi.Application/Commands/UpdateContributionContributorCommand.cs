namespace PerFi.Application.Commands;

public sealed record UpdateContributionContributorCommand(
    int ContributionContributorId,
    string Name);