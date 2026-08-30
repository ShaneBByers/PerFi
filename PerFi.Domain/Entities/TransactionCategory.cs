namespace PerFi.Domain.Entities;

public sealed record TransactionCategory
{
    public int Id { get; set; }
    public string Name { get; }
    public TransactionCategoryGroup Group { get; }
    public int DisplayOrder { get; set; }

    public TransactionCategory(string name, TransactionCategoryGroup group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(group);
        
        Name = name.Trim();
        Group = group;
    }

    public TransactionCategory(int id, string name, TransactionCategoryGroup group)
        : this(name, group)
    {
        Id = id;
    }
}
