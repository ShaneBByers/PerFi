namespace PerFi.Domain.Entities;

public sealed record Institution
{
    public int Id { get; set;}
    public string Name { get; }
    public IReadOnlyList<Account> Accounts { get; }

    public Institution(string name, IReadOnlyList<Account> accounts)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Institution name cannot be empty.", nameof(name));

        ArgumentNullException.ThrowIfNull(accounts, nameof(accounts));

        Name = name;
        Accounts = accounts;
    }

    public Institution(int id, string name, IReadOnlyList<Account> accounts)
        : this(name, accounts)
    {
        Id = id;
    }
}