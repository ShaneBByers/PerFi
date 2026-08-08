namespace PerFi.Infrastructure.Entities;

public class AccountBalanceEntity
{
    public int Id { get; set; }
    public required AccountEntity Account { get; set; }
    public required decimal Balance { get; set; }
}
