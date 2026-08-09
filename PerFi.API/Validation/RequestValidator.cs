namespace PerFi.API.Validation;

public static class RequestValidator
{
    public static IReadOnlyDictionary<string, string[]> ValidateCreateAccountRequest(string? accountName, int institutionId, int accountTypeId)
    {
        return ValidateAccountFields(accountName, institutionId, accountTypeId);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateAccountRequest(string? accountName, int institutionId, int accountTypeId)
    {
        return ValidateAccountFields(accountName, institutionId, accountTypeId);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateInstitutionRequest(string? institutionName)
    {
        return ValidateInstitutionName(institutionName);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateInstitutionRequest(string? institutionName)
    {
        return ValidateInstitutionName(institutionName);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateAccountTypeRequest(string? name, int accountTypeGroupId)
    {
        return ValidateAccountTypeFields(name, accountTypeGroupId);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateAccountTypeRequest(string? name, int accountTypeGroupId)
    {
        return ValidateAccountTypeFields(name, accountTypeGroupId);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateAccountTypeGroupRequest(string? name)
    {
        return ValidateAccountTypeGroupName(name);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateAccountTypeGroupRequest(string? name)
    {
        return ValidateAccountTypeGroupName(name);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateFinanceSnapshotRequest(DateOnly snapshotDate, IReadOnlyDictionary<int, decimal>? accountIdToBalanceMap)
    {
        return ValidateSnapshotFields(snapshotDate, accountIdToBalanceMap);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateFinanceSnapshotRequest(DateOnly snapshotDate, IReadOnlyDictionary<int, decimal>? accountIdToBalanceMap)
    {
        return ValidateSnapshotFields(snapshotDate, accountIdToBalanceMap);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateBulkUpdateFinanceSnapshotCellsRequest(IReadOnlyList<Requests.SnapshotCellUpdateRequest>? updates)
    {
        var errors = new Dictionary<string, string[]>();

        if (updates is null || updates.Count == 0)
        {
            errors[nameof(updates)] = ["At least one cell update is required."];
            return errors;
        }

        if (updates.Any(update => update.SnapshotId <= 0))
            errors[nameof(updates)] = ["All snapshot IDs must be greater than zero."];

        if (updates.Any(update => update.AccountId <= 0))
            errors[nameof(updates)] = ["All account IDs must be greater than zero."];

        return errors;
    }

    private static IReadOnlyDictionary<string, string[]> ValidateAccountFields(string? accountName, int institutionId, int accountTypeId)
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

    private static IReadOnlyDictionary<string, string[]> ValidateInstitutionName(string? institutionName)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(institutionName))
            errors[nameof(institutionName)] = ["Institution name is required."];

        return errors;
    }

    private static IReadOnlyDictionary<string, string[]> ValidateAccountTypeFields(string? name, int accountTypeGroupId)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
            errors[nameof(name)] = ["Account type name is required."];

        if (accountTypeGroupId <= 0)
            errors[nameof(accountTypeGroupId)] = ["Account type group ID must be greater than zero."];

        return errors;
    }

    private static IReadOnlyDictionary<string, string[]> ValidateAccountTypeGroupName(string? name)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
            errors[nameof(name)] = ["Account type group name is required."];

        return errors;
    }

    private static IReadOnlyDictionary<string, string[]> ValidateSnapshotFields(DateOnly snapshotDate, IReadOnlyDictionary<int, decimal>? accountIdToBalanceMap)
    {
        var errors = new Dictionary<string, string[]>();

        if (snapshotDate == default)
            errors[nameof(snapshotDate)] = ["Snapshot date is required."];

        if (accountIdToBalanceMap is null || accountIdToBalanceMap.Count == 0)
            errors[nameof(accountIdToBalanceMap)] = ["At least one account balance mapping is required."];

        if (accountIdToBalanceMap is not null)
        {
            var hasInvalidAccountId = accountIdToBalanceMap.Keys.Any(k => k <= 0);
            if (hasInvalidAccountId)
                errors[nameof(accountIdToBalanceMap)] = ["All account IDs must be greater than zero."];
        }

        return errors;
    }
}
