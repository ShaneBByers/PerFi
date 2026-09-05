namespace PerFi.Domain.Entities;

public sealed record Contribution
{
    public int Id { get; set; }
    public DateOnly Date { get; }
    public decimal Amount { get; }
    public ContributionContributorType Contributor { get; }
    public int AccountId { get; }

    public Contribution(DateOnly date, decimal amount, ContributionContributorType contributor, int accountId)
    {
        if (date == default)
            throw new ArgumentOutOfRangeException(nameof(date), "Contribution date must be provided.");

        if (!Enum.IsDefined(contributor))
            throw new ArgumentOutOfRangeException(nameof(contributor), "Contributor must be a valid contribution contributor type.");

        if (accountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(accountId), "Account ID must be greater than zero.");

        Date = date;
        Amount = amount;
        Contributor = contributor;
        AccountId = accountId;
    }

    public Contribution(int id, DateOnly date, decimal amount, ContributionContributorType contributor, int accountId)
        : this(date, amount, contributor, accountId)
    {
        Id = id;
    }
}
