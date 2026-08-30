namespace PerFi.Domain.Entities;

public sealed record Transaction
{
    public int Id { get; set; }
    public DateOnly Date { get; }
    public string CounterpartyName { get; }
    public decimal Amount { get; }
    public string? Description { get; }
    public TransactionCategory Category { get; }
    public int AccountId { get; }

    public Transaction(
        DateOnly date,
        string counterpartyName,
        decimal amount,
        TransactionCategory category,
        int accountId,
        string? description = null)
    {
        if (date == default)
            throw new ArgumentOutOfRangeException(nameof(date), "Transaction date must be provided.");

        ArgumentException.ThrowIfNullOrWhiteSpace(counterpartyName, nameof(counterpartyName));

        ArgumentNullException.ThrowIfNull(category);

        if (accountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(accountId), "Account ID must be greater than zero.");

        Date = date;
        CounterpartyName = counterpartyName.Trim();
        Amount = amount;
        Category = category;
        AccountId = accountId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public Transaction(
        int id,
        DateOnly date,
        string counterpartyName,
        decimal amount,
        TransactionCategory category,
        int accountId,
        string? description = null)
        : this(date, counterpartyName, amount, category, accountId, description)
    {
        Id = id;
    }
}