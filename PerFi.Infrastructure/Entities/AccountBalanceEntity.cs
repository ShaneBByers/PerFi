namespace PerFi.Infrastructure.Entities;

public class AccountBalanceEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public required AccountEntity Account { get; set; }
    public int FinanceSnapshotId { get; set; }
    public FinanceSnapshotEntity? FinanceSnapshot { get; set; }
    public required decimal Balance { get; set; }
}
