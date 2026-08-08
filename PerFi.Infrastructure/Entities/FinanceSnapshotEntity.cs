namespace PerFi.Infrastructure.Entities;

public class FinanceSnapshotEntity
{
    public int Id { get; set; }
    public required DateOnly Date { get; set; }
    public required ICollection<AccountBalanceEntity> AccountBalances { get; set; }
}
