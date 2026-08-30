namespace PerFi.Domain.Entities;

public sealed record ContributionContributor
{
    public int Id { get; set; }
    public string Name { get; }
    public int DisplayOrder { get; set; }

    public ContributionContributor(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        
        Name = name.Trim();
    }

    public ContributionContributor(int id, string name)
        : this(name)
    {
        Id = id;
    }
}
