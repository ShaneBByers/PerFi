namespace PerFi.Infrastructure.Entities;

public class AccountTypeGroupEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public required ICollection<AccountTypeEntity> AccountTypes { get; set; }
}