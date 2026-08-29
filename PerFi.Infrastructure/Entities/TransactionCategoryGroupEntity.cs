namespace PerFi.Infrastructure.Entities;

public class TransactionCategoryGroupEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public required string UserId { get; set; }
    public ICollection<TransactionCategoryEntity> TransactionCategories { get; set; } = [];
}
