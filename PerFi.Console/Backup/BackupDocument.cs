using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PerFi.Console.Backup;

// Full-fidelity, human-readable snapshot of one user's data, keyed by name (not DB id) so it can be
// edited by hand and restored into any database without caring what the original row IDs were.
public sealed record BackupDocument(
    string SchemaVersion,
    DateTimeOffset ExportedAtUtc,
    string Username,
    IReadOnlyList<BackupAccountTypeGroup> AccountTypeGroups,
    IReadOnlyList<BackupInstitution> Institutions,
    IReadOnlyList<BackupFinanceSnapshot> FinanceSnapshots,
    IReadOnlyList<BackupTransactionCategoryGroup> TransactionCategoryGroups,
    IReadOnlyList<BackupTransaction> Transactions,
    IReadOnlyList<BackupContributionContributor> ContributionContributors,
    IReadOnlyList<BackupContribution> Contributions)
{
    public const string CurrentSchemaVersion = "1.0";
}

public sealed record BackupAccountTypeGroup(string Name, int DisplayOrder, IReadOnlyList<BackupAccountType> AccountTypes);

public sealed record BackupAccountType(string Name, int DisplayOrder);

public sealed record BackupInstitution(string Name, int DisplayOrder, IReadOnlyList<BackupAccount> Accounts);

public sealed record BackupAccount(string Name, int DisplayOrder, string AccountTypeGroup, string AccountType);

public sealed record BackupFinanceSnapshot(DateOnly Date, IReadOnlyList<BackupAccountBalance> AccountBalances);

public sealed record BackupAccountBalance(string Institution, string Account, decimal Balance);

public sealed record BackupTransactionCategoryGroup(string Name, int DisplayOrder, IReadOnlyList<BackupTransactionCategory> Categories);

public sealed record BackupTransactionCategory(string Name, int DisplayOrder);

public sealed record BackupTransaction(
    DateOnly Date,
    string Counterparty,
    decimal Amount,
    string CategoryGroup,
    string Category,
    string Institution,
    string Account,
    string? Description);

public sealed record BackupContributionContributor(string Name, int DisplayOrder);

public sealed record BackupContribution(
    DateOnly Date,
    decimal Amount,
    string Contributor,
    string Institution,
    string Account);

public static class BackupJsonOptions
{
    // This file is only ever read/written by the CLI, never rendered in a browser, so relaxed
    // escaping (no \u0026 for '&', etc.) is safe here and keeps hand-edited backups readable.
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
