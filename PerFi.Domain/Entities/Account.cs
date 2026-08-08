namespace PerFi.Domain.Entities;

public sealed record Account
{
    public int Id { get; set; }
    public string Name { get; }
    public AccountType Type { get; }

    public Account(string name, AccountType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty.", nameof(name));

        Name = name;
        Type = type;
    }

    public Account(int id, string name, AccountType type)
        : this(name, type)
    {
        Id = id;
    }
}