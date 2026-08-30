using System.Text.Json;
using PerFi.Application.Interfaces;
using PerFi.Console.Backup;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using PerFi.Infrastructure.Entities;

namespace PerFi.Console.Operations;

public sealed class ExportBackupOperation(
    UserManager<ApplicationUser> userManager,
    ConsoleCurrentUserService currentUser,
    IAccountTypeGroupService accountTypeGroupService,
    IAccountTypeService accountTypeService,
    IInstitutionService institutionService,
    IFinanceSnapshotService financeSnapshotService,
    ITransactionCategoryGroupService transactionCategoryGroupService,
    ITransactionCategoryService transactionCategoryService,
    ITransactionService transactionService,
    IContributionContributorService contributionContributorService,
    IContributionService contributionService)
{
    public async Task ExecuteAsync(string outputPath, string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var user = await userManager.FindByNameAsync(username);
        if (user is null)
            throw new InvalidOperationException($"User '{username}' was not found.");

        currentUser.UserId = user.Id;

        var resolvedPath = Path.GetFullPath(outputPath);

        System.Console.WriteLine($"Exporting backup for user: {username}");
        System.Console.WriteLine($"Output path: {resolvedPath}");
        System.Console.WriteLine();

        var accountTypeGroups = await accountTypeGroupService.GetAllAccountTypeGroupsAsync(cancellationToken);
        var accountTypes = await accountTypeService.GetAllAccountTypesAsync(cancellationToken);
        var institutions = await institutionService.GetAllInstitutionsAsync(cancellationToken);
        var snapshots = await financeSnapshotService.GetAllSnapshotsAsync(cancellationToken);
        var categoryGroups = await transactionCategoryGroupService.GetAllTransactionCategoryGroupsAsync(cancellationToken);
        var categories = await transactionCategoryService.GetAllTransactionCategoriesAsync(cancellationToken);
        var transactions = await transactionService.GetAllTransactionsAsync(cancellationToken);
        var contributors = await contributionContributorService.GetAllContributionContributorsAsync(cancellationToken);
        var contributions = await contributionService.GetAllContributionsAsync(cancellationToken);

        var accountLookup = BuildAccountLookup(institutions);

        var document = new BackupDocument(
            BackupDocument.CurrentSchemaVersion,
            DateTimeOffset.UtcNow,
            username,
            BuildAccountTypeGroups(accountTypeGroups, accountTypes),
            BuildInstitutions(institutions),
            BuildFinanceSnapshots(snapshots, accountLookup),
            BuildTransactionCategoryGroups(categoryGroups, categories),
            BuildTransactions(transactions, accountLookup),
            [.. contributors.Select(contributor => new BackupContributionContributor(contributor.Name, contributor.DisplayOrder))],
            BuildContributions(contributions, accountLookup));

        Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath) is { Length: > 0 } directory ? directory : ".");
        await using (var stream = File.Create(resolvedPath))
        {
            await JsonSerializer.SerializeAsync(stream, document, BackupJsonOptions.Default, cancellationToken);
        }

        System.Console.WriteLine("Export summary");
        System.Console.WriteLine($"Account type groups: {accountTypeGroups.Count}");
        System.Console.WriteLine($"Institutions: {institutions.Count}");
        System.Console.WriteLine($"Snapshots: {snapshots.Count}");
        System.Console.WriteLine($"Transaction category groups: {categoryGroups.Count}");
        System.Console.WriteLine($"Transactions: {transactions.Count}");
        System.Console.WriteLine($"Contribution contributors: {contributors.Count}");
        System.Console.WriteLine($"Contributions: {contributions.Count}");
        System.Console.WriteLine();
        System.Console.WriteLine($"Backup written to {resolvedPath}");
    }

    private static Dictionary<int, (string Institution, string Account)> BuildAccountLookup(
        IReadOnlyList<Institution> institutions)
    {
        var lookup = new Dictionary<int, (string Institution, string Account)>();

        foreach (var institution in institutions)
        {
            foreach (var account in institution.Accounts)
            {
                lookup[account.Id] = (institution.Name, account.Name);
            }
        }

        return lookup;
    }

    private static IReadOnlyList<BackupAccountTypeGroup> BuildAccountTypeGroups(
        IReadOnlyList<AccountTypeGroup> accountTypeGroups,
        IReadOnlyList<AccountType> accountTypes)
        => [.. accountTypeGroups.Select(group => new BackupAccountTypeGroup(
            group.Name,
            group.DisplayOrder,
            [.. accountTypes
                .Where(type => type.Group.Id == group.Id)
                .Select(type => new BackupAccountType(type.Name, type.DisplayOrder))]))];

    private static IReadOnlyList<BackupInstitution> BuildInstitutions(IReadOnlyList<Institution> institutions)
        => [.. institutions.Select(institution => new BackupInstitution(
            institution.Name,
            institution.DisplayOrder,
            [.. institution.Accounts.Select(account => new BackupAccount(
                account.Name,
                account.DisplayOrder,
                account.Type.Group.Name,
                account.Type.Name))]))];

    private static IReadOnlyList<BackupFinanceSnapshot> BuildFinanceSnapshots(
        IReadOnlyList<FinanceSnapshot> snapshots,
        IReadOnlyDictionary<int, (string Institution, string Account)> accountLookup)
        => [.. snapshots
            .OrderBy(snapshot => snapshot.Date)
            .Select(snapshot => new BackupFinanceSnapshot(
                snapshot.Date,
                [.. snapshot.AccountBalances.Select(balance =>
                {
                    var (institutionName, accountName) = accountLookup[balance.Account.Id];
                    return new BackupAccountBalance(institutionName, accountName, balance.Balance);
                })]))];

    private static IReadOnlyList<BackupTransactionCategoryGroup> BuildTransactionCategoryGroups(
        IReadOnlyList<TransactionCategoryGroup> categoryGroups,
        IReadOnlyList<TransactionCategory> categories)
        => [.. categoryGroups.Select(group => new BackupTransactionCategoryGroup(
            group.Name,
            group.DisplayOrder,
            [.. categories
                .Where(category => category.Group.Id == group.Id)
                .Select(category => new BackupTransactionCategory(category.Name, category.DisplayOrder))]))];

    private static IReadOnlyList<BackupTransaction> BuildTransactions(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<int, (string Institution, string Account)> accountLookup)
        => [.. transactions
            .OrderBy(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Id)
            .Select(transaction =>
            {
                var (institutionName, accountName) = accountLookup[transaction.AccountId];
                return new BackupTransaction(
                    transaction.Date,
                    transaction.CounterpartyName,
                    transaction.Amount,
                    transaction.Category.Group.Name,
                    transaction.Category.Name,
                    institutionName,
                    accountName,
                    transaction.Description);
            })];

    private static IReadOnlyList<BackupContribution> BuildContributions(
        IReadOnlyList<Contribution> contributions,
        IReadOnlyDictionary<int, (string Institution, string Account)> accountLookup)
        => [.. contributions
            .OrderBy(contribution => contribution.Date)
            .ThenBy(contribution => contribution.Id)
            .Select(contribution =>
            {
                var (institutionName, accountName) = accountLookup[contribution.AccountId];
                return new BackupContribution(
                    contribution.Date,
                    contribution.Amount,
                    contribution.Contributor.Name,
                    institutionName,
                    accountName);
            })];
}
