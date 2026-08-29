namespace PerFi.Infrastructure.Entities;

public class TransactionCategoryEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public required string UserId { get; set; }
    public int TransactionCategoryGroupId { get; set; }
    public TransactionCategoryGroupEntity TransactionCategoryGroup { get; set; } = null!;
    public ICollection<TransactionEntity> Transactions { get; set; } = [];
}
