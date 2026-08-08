namespace PerFi.Domain.Entities;

public sealed record FinanceSnapshot
{
    public int Id { get; set; }
    public DateOnly Date { get; }
    public IReadOnlyList<AccountBalance> AccountBalances { get; }

    public FinanceSnapshot(DateOnly date, IReadOnlyList<AccountBalance> accountBalances)
    {
        ArgumentNullException.ThrowIfNull(accountBalances, nameof(accountBalances));

        if (accountBalances.Count == 0)
            throw new ArgumentException("A snapshot must contain at least one account balance.", nameof(accountBalances));

        if (accountBalances.GroupBy(ab => ab.Account.Id).Any(g => g.Count() > 1))
            throw new ArgumentException("Account balances cannot contain duplicate accounts.", nameof(accountBalances));

        if (date == default)
            throw new ArgumentOutOfRangeException(nameof(date), "Snapshot date must be provided.");

        Date = date;
        AccountBalances = accountBalances;
    }

    public FinanceSnapshot(int id, DateOnly date, IReadOnlyList<AccountBalance> accountBalances)
        : this(date, accountBalances)
    {
        Id = id;
    }
}
