namespace PerFi.Domain.Entities;

public sealed record AccountTypeGroup
{
    public int Id { get; set; }
    public string Name { get; }

    public AccountTypeGroup(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
    }

    public AccountTypeGroup(int id, string name)
        : this(name)
    {
        Id = id;
    }
}