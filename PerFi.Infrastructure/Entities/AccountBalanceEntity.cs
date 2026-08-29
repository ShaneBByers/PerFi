namespace PerFi.Infrastructure.Entities;

public class AccountBalanceEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public required string UserId { get; set; }
    public AccountEntity Account { get; set; } = null!;
    public int FinanceSnapshotId { get; set; }
    public FinanceSnapshotEntity FinanceSnapshot { get; set; } = null!;
    public required decimal Balance { get; set; }
}
