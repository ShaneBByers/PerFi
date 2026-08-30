namespace PerFi.Infrastructure.Entities;

public class ContributionContributorEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public required string UserId { get; set; }
    public ICollection<ContributionEntity> Contributions { get; set; } = [];
}
