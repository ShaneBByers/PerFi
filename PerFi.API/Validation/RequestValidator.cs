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

    public static IReadOnlyDictionary<string, string[]> ValidateCreateContributionContributorRequest(string? name)
    {
        return ValidateContributionContributorName(name);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateContributionContributorRequest(string? name)
    {
        return ValidateContributionContributorName(name);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateTransactionCategoryGroupRequest(string? name)
    {
        return ValidateTransactionCategoryGroupName(name);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateTransactionCategoryGroupRequest(string? name)
    {
        return ValidateTransactionCategoryGroupName(name);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateContributionRequest(DateOnly date, decimal amount, int contributionContributorId, int accountId)
    {
        return ValidateContributionFields(date, amount, contributionContributorId, accountId);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateContributionRequest(DateOnly date, decimal amount, int contributionContributorId, int accountId)
    {
        return ValidateContributionFields(date, amount, contributionContributorId, accountId);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateTransactionCategoryRequest(string? name, int transactionCategoryGroupId)
    {
        return ValidateTransactionCategoryFields(name, transactionCategoryGroupId);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateTransactionCategoryRequest(string? name, int transactionCategoryGroupId)
    {
        return ValidateTransactionCategoryFields(name, transactionCategoryGroupId);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateCreateTransactionRequest(DateOnly date, string? counterpartyName, decimal amount, int transactionCategoryId, int accountId)
    {
        return ValidateTransactionFields(date, counterpartyName, amount, transactionCategoryId, accountId);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateUpdateTransactionRequest(DateOnly date, string? counterpartyName, decimal amount, int transactionCategoryId, int accountId)
    {
        return ValidateTransactionFields(date, counterpartyName, amount, transactionCategoryId, accountId);
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

    private static IReadOnlyDictionary<string, string[]> ValidateContributionContributorName(string? name)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
            errors[nameof(name)] = ["Contribution contributor name is required."];

        return errors;
    }

    private static IReadOnlyDictionary<string, string[]> ValidateTransactionCategoryGroupName(string? name)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
            errors[nameof(name)] = ["Transaction category group name is required."];

        return errors;
    }

    private static IReadOnlyDictionary<string, string[]> ValidateContributionFields(DateOnly date, decimal amount, int contributionContributorId, int accountId)
    {
        var errors = new Dictionary<string, string[]>();

        if (date == default)
            errors[nameof(date)] = ["Contribution date is required."];

        if (amount == 0)
            errors[nameof(amount)] = ["Contribution amount is required."];

        if (contributionContributorId <= 0)
            errors[nameof(contributionContributorId)] = ["Contribution contributor ID must be greater than zero."];

        if (accountId <= 0)
            errors[nameof(accountId)] = ["Account ID must be greater than zero."];

        return errors;
    }

    private static IReadOnlyDictionary<string, string[]> ValidateTransactionCategoryFields(string? name, int transactionCategoryGroupId)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
            errors[nameof(name)] = ["Transaction category name is required."];

        if (transactionCategoryGroupId <= 0)
            errors[nameof(transactionCategoryGroupId)] = ["Transaction category group ID must be greater than zero."];

        return errors;
    }

    private static IReadOnlyDictionary<string, string[]> ValidateTransactionFields(DateOnly date, string? counterpartyName, decimal amount, int transactionCategoryId, int accountId)
    {
        var errors = new Dictionary<string, string[]>();

        if (date == default)
            errors[nameof(date)] = ["Transaction date is required."];

        if (string.IsNullOrWhiteSpace(counterpartyName))
            errors[nameof(counterpartyName)] = ["Counterparty name is required."];

        if (amount == 0)
            errors[nameof(amount)] = ["Transaction amount is required."];

        if (transactionCategoryId <= 0)
            errors[nameof(transactionCategoryId)] = ["Transaction category ID must be greater than zero."];

        if (accountId <= 0)
            errors[nameof(accountId)] = ["Account ID must be greater than zero."];

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
