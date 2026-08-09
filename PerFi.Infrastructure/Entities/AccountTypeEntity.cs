namespace PerFi.Infrastructure.Entities;

public class AccountTypeEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int AccountTypeGroupId { get; set; }
    public required AccountTypeGroupEntity AccountTypeGroup { get; set; }
    public ICollection<AccountEntity> Accounts { get; set; } = [];
}
