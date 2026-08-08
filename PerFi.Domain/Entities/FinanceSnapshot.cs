namespace PerFi.Domain.Entities;

public record FinanceSnapshot(
    DateOnly Date,
    IReadOnlyList<AccountBalance> Balances);