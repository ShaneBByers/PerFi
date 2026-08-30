namespace PerFi.Domain.Entities;

public sealed record TransactionCategoryGroup
{
    public int Id { get; set; }
    public string Name { get; }
    public int DisplayOrder { get; set; }

    public TransactionCategoryGroup(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        
        Name = name.Trim();
    }

    public TransactionCategoryGroup(int id, string name)
        : this(name)
    {
        Id = id;
    }
}
