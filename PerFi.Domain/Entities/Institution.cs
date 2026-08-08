namespace PerFi.Domain.Entities;

public sealed record Institution
{
    public int Id { get; set; }
    public string Name { get; }
    public IReadOnlyList<Account> Accounts { get; }

    public Institution(string name, IReadOnlyList<Account> accounts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(accounts, nameof(accounts));

        Name = name.Trim();
        Accounts = accounts;
    }

    public Institution(int id, string name, IReadOnlyList<Account> accounts)
        : this(name, accounts)
    {
        Id = id;
    }
}