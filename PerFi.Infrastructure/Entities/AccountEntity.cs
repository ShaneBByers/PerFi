namespace PerFi.Infrastructure.Entities;

public class AccountEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int InstitutionId { get; set; }
    public InstitutionEntity? Institution { get; set; }
    public int AccountTypeId { get; set; }
    public required AccountTypeEntity AccountType { get; set; }
}
