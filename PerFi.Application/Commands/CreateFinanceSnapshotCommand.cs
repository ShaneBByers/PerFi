namespace PerFi.Application.Commands;

public record CreateFinanceSnapshotCommand(
    DateOnly SnapshotDate,
    IReadOnlyDictionary<int, decimal> AccountIdToBalanceMap);