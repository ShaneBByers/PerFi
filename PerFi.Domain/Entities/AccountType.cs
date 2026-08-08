namespace PerFi.Domain.Entities;

public sealed record AccountType
{
    public int Id { get; set; }
    public string Name { get; }

    public AccountType(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
    }

    public AccountType(int id, string name)
        : this(name)
    {
        Id = id;
    }
}