namespace PerFi.Domain.Entities;

public sealed record AccountBalance
{
    public Account Account { get; }
    public decimal Balance { get; }

    public AccountBalance(Account account, decimal balance)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (balance < 0)
            throw new ArgumentOutOfRangeException(nameof(balance), "Balance cannot be negative.");

        Account = account;
        Balance = balance;
    }
}