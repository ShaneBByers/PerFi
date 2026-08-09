namespace PerFi.Domain.Entities;

public sealed record AccountBalance
{
    public Account Account { get; }
    public decimal Balance { get; }

    public AccountBalance(Account account, decimal balance)
    {
        ArgumentNullException.ThrowIfNull(account);

        Account = account;
        Balance = balance;
    }
}