namespace PerFi.API.Validation;

public static class RequestValidator
{
    public static IReadOnlyDictionary<string, string[]> ValidateCreateAccountRequest(string? accountName, int institutionId, int accountTypeId)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(accountName))
            errors[nameof(accountName)] = ["Account name is required."];

        if (institutionId <= 0)
            errors[nameof(institutionId)] = ["Institution ID must be greater than zero."];

        if (accountTypeId <= 0)
            errors[nameof(accountTypeId)] = ["Account type ID must be greater than zero."];

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateInstitutionRequest(string? institutionName)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(institutionName))
            errors[nameof(institutionName)] = ["Institution name is required."];

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateAccountTypeRequest(string? name)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
            errors[nameof(name)] = ["Account type name is required."];

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateFinanceSnapshotRequest(DateOnly snapshotDate, IReadOnlyDictionary<int, decimal>? accountIdToBalanceMap)
    {
        var errors = new Dictionary<string, string[]>();

        if (snapshotDate == default)
            errors[nameof(snapshotDate)] = ["Snapshot date is required."];

        if (accountIdToBalanceMap is null || accountIdToBalanceMap.Count == 0)
            errors[nameof(accountIdToBalanceMap)] = ["At least one account balance mapping is required."];

        return errors;
    }
}
