namespace PerFi.API.Responses;

public sealed record ProjectionGroupResponse(
    string GroupName,
    ProjectionAmountsResponse Amounts,
    ProjectionAmountsResponse AmountsInTodaysDollars,
    ProjectionAmountsResponse? ExpectedAmounts,
    ProjectionAmountsResponse? ExpectedAmountsInTodaysDollars);
