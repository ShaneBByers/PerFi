namespace PerFi.API.Responses;

public sealed record ProjectionAmountsResponse(
    decimal StartingAmount,
    decimal ContributedAmount,
    decimal GrowthAmount,
    decimal FinalAmount);
