namespace PerFi.Domain.Entities;

public sealed record AccountBalance
{
    public Account Account { get; }
    public double Balance { get; }

    public AccountBalance(Account account, double balance)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!double.IsFinite(balance))
            throw new ArgumentException("Balance must be a finite number.", nameof(balance));

        Account = account;
        Balance = balance;
    }
}