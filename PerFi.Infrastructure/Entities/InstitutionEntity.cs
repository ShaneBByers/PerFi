namespace PerFi.Infrastructure.Entities;

public class InstitutionEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public required string UserId { get; set; }
    public required ICollection<AccountEntity> Accounts { get; set; }
}
