namespace PerFi.Domain.Entities;

public sealed record AccountType
{
    public int Id { get; set; }
    public string Name { get; }
    public AccountTypeGroup Group { get; }

    public AccountType(string name, AccountTypeGroup group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(group);
        Name = name.Trim();
        Group = group;
    }

    public AccountType(int id, string name, AccountTypeGroup group)
        : this(name, group)
    {
        Id = id;
    }
}