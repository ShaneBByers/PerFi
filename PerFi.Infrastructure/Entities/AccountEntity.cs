namespace PerFi.Infrastructure.Entities;

public class AccountEntity
{
    public int Id { get; set; }
    public required string AccountName { get; set; }
    public required InstitutionEntity Institution { get; set; }
    public required AccountTypeEntity AccountType { get; set; }
}
