namespace PerFi.Infrastructure.Entities;

public class AccountEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public required string UserId { get; set; }
    public int InstitutionId { get; set; }
    public InstitutionEntity Institution { get; set; } = null!;
    public int AccountTypeId { get; set; }
    public AccountTypeEntity AccountType { get; set; } = null!;
    public ICollection<AccountBalanceEntity> AccountBalances { get; set; } = [];
    public ICollection<TransactionEntity> Transactions { get; set; } = [];
    public ICollection<ContributionEntity> Contributions { get; set; } = [];
}
