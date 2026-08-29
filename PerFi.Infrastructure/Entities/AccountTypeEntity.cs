namespace PerFi.Infrastructure.Entities;

public class AccountTypeEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public required string UserId { get; set; }
    public int AccountTypeGroupId { get; set; }
    public AccountTypeGroupEntity AccountTypeGroup { get; set; } = null!;
    public ICollection<AccountEntity> Accounts { get; set; } = [];
}
