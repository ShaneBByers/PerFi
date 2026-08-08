namespace PerFi.Application.Commands;

public record CreateFinanceSnapshotCommand(
    DateOnly SnapshotDate,
    IReadOnlyDictionary<string, double> AccountNameToBalanceMap);