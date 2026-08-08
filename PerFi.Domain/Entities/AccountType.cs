namespace PerFi.Domain.Entities;

public sealed record AccountType
{
    public int Id { get; set; }
    public string Name { get; }

    public AccountType(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account type name cannot be empty.", nameof(name));

        Name = name;
    }

    public AccountType(int id, string name)
        : this(name)
    {
        Id = id;
    }
}