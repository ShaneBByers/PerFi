namespace PerFi.Infrastructure.Entities;

public class TransactionEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public required string CounterpartyName { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public required string UserId { get; set; }
    public int TransactionCategoryId { get; set; }
    public TransactionCategoryEntity TransactionCategory { get; set; } = null!;
    public int AccountId { get; set; }
    public AccountEntity Account { get; set; } = null!;
}
