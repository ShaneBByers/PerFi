namespace PerFi.Domain.Entities;

public sealed record Account
{
    public int Id { get; set; }
    public string Name { get; }
    public AccountType Type { get; }
    public int InstitutionId { get; set; }

    public Account(string name, AccountType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(type);

        Name = name.Trim();
        Type = type;
    }

    public Account(int id, string name, AccountType type, int institutionId = 0)
        : this(name, type)
    {
        Id = id;
        InstitutionId = institutionId;
    }
}