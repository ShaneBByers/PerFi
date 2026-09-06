namespace PerFi.Domain.Entities.Projections;

// GrowthAmount is always the residual: FinalAmount - StartingAmount - ContributedAmount.
public sealed record ProjectionAmounts(
    decimal StartingAmount,
    decimal ContributedAmount,
    decimal GrowthAmount,
    decimal FinalAmount);
