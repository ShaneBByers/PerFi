namespace PerFi.Infrastructure.Entities;

public class ContributorEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string UserId { get; set; }
    public ICollection<ContributionEntity> Contributions { get; set; } = [];
}
