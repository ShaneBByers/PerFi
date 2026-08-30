namespace PerFi.Infrastructure.Entities;

public class ContributionEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public required string UserId { get; set; }
    public int ContributorId { get; set; }
    public ContributionContributorEntity Contributor { get; set; } = null!;
    public int AccountId { get; set; }
    public AccountEntity Account { get; set; } = null!;
}
