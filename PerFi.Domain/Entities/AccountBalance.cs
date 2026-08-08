namespace PerFi.Domain.Entities;

public record AccountBalance(
    Account Account,
    double Balance);