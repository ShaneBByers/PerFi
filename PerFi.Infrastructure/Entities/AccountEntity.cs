namespace PerFi.Infrastructure.Entities;

public class AccountEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required AccountTypeEntity Type { get; set; }
}
