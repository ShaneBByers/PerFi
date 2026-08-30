using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Console.Backup;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;

namespace PerFi.Console.Operations;

public sealed class ImportBackupOperation(
    PerFiDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ConsoleCurrentUserService currentUser,
    IAccountTypeGroupService accountTypeGroupService,
    IAccountTypeService accountTypeService,
    IInstitutionService institutionService,
    IAccountService accountService,
    IFinanceSnapshotService financeSnapshotService,
    ITransactionCategoryGroupService transactionCategoryGroupService,
    ITransactionCategoryService transactionCategoryService,
    ITransactionService transactionService,
    IContributionContributorService contributionContributorService,
    IContributionService contributionService)
{
    public async Task ExecuteAsync(string backupPath, string username, bool dryRun, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var user = await userManager.FindByNameAsync(username);
        if (user is null)
            throw new InvalidOperationException($"User '{username}' was not found. Run 'create-user {username} <password>' first.");

        currentUser.UserId = user.Id;

        var resolvedPath = Path.GetFullPath(backupPath);
        if (!File.Exists(resolvedPath))
            throw new FileNotFoundException($"Backup file '{resolvedPath}' was not found.", resolvedPath);

        System.Console.WriteLine($"Backup path: {resolvedPath}");
        System.Console.WriteLine($"Restoring for user: {username}");
        System.Console.WriteLine($"Mode: {(dryRun ? "dry-run" : "import")}");
        System.Console.WriteLine();

        BackupDocument document;
        await using (var stream = File.OpenRead(resolvedPath))
        {
            document = await JsonSerializer.DeserializeAsync<BackupDocument>(stream, BackupJsonOptions.Default, cancellationToken)
                ?? throw new InvalidOperationException("Backup file could not be parsed.");
        }

        if (!string.Equals(document.SchemaVersion, BackupDocument.CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Backup schema version '{document.SchemaVersion}' is not supported. Expected '{BackupDocument.CurrentSchemaVersion}'.");
        }

        var hasExistingData = await DatabaseHasExistingDataAsync(cancellationToken);

        PrintSummary(document, hasExistingData, dryRun);

        if (hasExistingData)
        {
            throw new InvalidOperationException(
                $"Restore requires an empty account for user '{username}'. Run 'reset-database' first.");
        }

        if (dryRun)
        {
            System.Console.WriteLine("Dry run completed. No changes were written to the database.");
            return;
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
            await RestoreAsync(document, cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
        });

        System.Console.WriteLine();
        System.Console.WriteLine("Restore completed.");
    }

    private async Task<bool> DatabaseHasExistingDataAsync(CancellationToken cancellationToken)
        => await dbContext.AccountTypeGroups.AnyAsync(g => g.UserId == currentUser.UserId, cancellationToken)
            || await dbContext.Institutions.AnyAsync(i => i.UserId == currentUser.UserId, cancellationToken)
            || await dbContext.FinanceSnapshots.AnyAsync(s => s.UserId == currentUser.UserId, cancellationToken)
            || await dbContext.TransactionCategoryGroups.AnyAsync(g => g.UserId == currentUser.UserId, cancellationToken)
            || await dbContext.Transactions.AnyAsync(t => t.UserId == currentUser.UserId, cancellationToken)
            || await dbContext.ContributionContributors.AnyAsync(c => c.UserId == currentUser.UserId, cancellationToken)
            || await dbContext.Contributions.AnyAsync(c => c.UserId == currentUser.UserId, cancellationToken);

    private async Task RestoreAsync(BackupDocument document, CancellationToken cancellationToken)
    {
        var accountTypeGroupIds = await CreateAccountTypeGroupsAsync(document.AccountTypeGroups, cancellationToken);
        await ReorderAccountTypeGroupsAsync(document.AccountTypeGroups, accountTypeGroupIds, cancellationToken);

        var accountTypeIds = await CreateAccountTypesAsync(document.AccountTypeGroups, accountTypeGroupIds, cancellationToken);
        await ReorderAccountTypesAsync(document.AccountTypeGroups, accountTypeIds, cancellationToken);

        var institutionIds = await CreateInstitutionsAsync(document.Institutions, cancellationToken);
        await ReorderInstitutionsAsync(document.Institutions, institutionIds, cancellationToken);

        var accountIds = await CreateAccountsAsync(document.Institutions, institutionIds, accountTypeIds, cancellationToken);
        await ReorderAccountsAsync(document.Institutions, accountIds, cancellationToken);

        await CreateFinanceSnapshotsAsync(document.FinanceSnapshots, accountIds, cancellationToken);

        var categoryGroupIds = await CreateTransactionCategoryGroupsAsync(document.TransactionCategoryGroups, cancellationToken);
        await ReorderTransactionCategoryGroupsAsync(document.TransactionCategoryGroups, categoryGroupIds, cancellationToken);

        var categoryIds = await CreateTransactionCategoriesAsync(document.TransactionCategoryGroups, categoryGroupIds, cancellationToken);
        await ReorderTransactionCategoriesAsync(document.TransactionCategoryGroups, categoryIds, cancellationToken);

        await CreateTransactionsAsync(document.Transactions, categoryIds, accountIds, cancellationToken);

        var contributorIds = await CreateContributionContributorsAsync(document.ContributionContributors, cancellationToken);
        await ReorderContributionContributorsAsync(document.ContributionContributors, contributorIds, cancellationToken);

        await CreateContributionsAsync(document.Contributions, contributorIds, accountIds, cancellationToken);
    }

    private async Task<Dictionary<string, int>> CreateAccountTypeGroupsAsync(
        IReadOnlyList<BackupAccountTypeGroup> groups,
        CancellationToken cancellationToken)
    {
        var groupIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var result = await accountTypeGroupService.CreateAccountTypeGroupAsync(
                new CreateAccountTypeGroupCommand(group.Name),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
                throw new InvalidOperationException($"Failed to create account type group '{group.Name}': {result.Error}");

            groupIds[NormalizeKey(group.Name)] = result.Value.Id;
        }

        return groupIds;
    }

    private async Task ReorderAccountTypeGroupsAsync(
        IReadOnlyList<BackupAccountTypeGroup> groups,
        IReadOnlyDictionary<string, int> accountTypeGroupIds,
        CancellationToken cancellationToken)
    {
        var orderedIds = groups
            .OrderBy(group => group.DisplayOrder)
            .Select(group => accountTypeGroupIds[NormalizeKey(group.Name)])
            .ToList();

        var result = await accountTypeGroupService.ReorderAccountTypeGroupsAsync(
            new ReorderAccountTypeGroupCommand(orderedIds),
            cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to restore account type group order: {result.Error}");
    }

    private async Task<Dictionary<string, int>> CreateAccountTypesAsync(
        IReadOnlyList<BackupAccountTypeGroup> groups,
        IReadOnlyDictionary<string, int> accountTypeGroupIds,
        CancellationToken cancellationToken)
    {
        var accountTypeIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var groupId = accountTypeGroupIds[NormalizeKey(group.Name)];

            foreach (var accountType in group.AccountTypes)
            {
                var result = await accountTypeService.CreateAccountTypeAsync(
                    new CreateAccountTypeCommand(accountType.Name, groupId),
                    cancellationToken);

                if (result.IsFailure || result.Value is null)
                    throw new InvalidOperationException($"Failed to create account type '{accountType.Name}': {result.Error}");

                accountTypeIds[NormalizeKey(accountType.Name)] = result.Value.Id;
            }
        }

        return accountTypeIds;
    }

    // AccountType.DisplayOrder is a single sequence per user, not per group, so it is reordered across all groups at once.
    private async Task ReorderAccountTypesAsync(
        IReadOnlyList<BackupAccountTypeGroup> groups,
        IReadOnlyDictionary<string, int> accountTypeIds,
        CancellationToken cancellationToken)
    {
        var orderedIds = groups
            .SelectMany(group => group.AccountTypes)
            .OrderBy(accountType => accountType.DisplayOrder)
            .Select(accountType => accountTypeIds[NormalizeKey(accountType.Name)])
            .ToList();

        var result = await accountTypeService.ReorderAccountTypesAsync(
            new ReorderAccountTypeCommand(orderedIds),
            cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to restore account type order: {result.Error}");
    }

    private async Task<Dictionary<string, int>> CreateInstitutionsAsync(
        IReadOnlyList<BackupInstitution> institutions,
        CancellationToken cancellationToken)
    {
        var institutionIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var institution in institutions)
        {
            var result = await institutionService.CreateInstitutionAsync(
                new CreateInstitutionCommand(institution.Name),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
                throw new InvalidOperationException($"Failed to create institution '{institution.Name}': {result.Error}");

            institutionIds[NormalizeKey(institution.Name)] = result.Value.Id;
        }

        return institutionIds;
    }

    private async Task ReorderInstitutionsAsync(
        IReadOnlyList<BackupInstitution> institutions,
        IReadOnlyDictionary<string, int> institutionIds,
        CancellationToken cancellationToken)
    {
        var orderedIds = institutions
            .OrderBy(institution => institution.DisplayOrder)
            .Select(institution => institutionIds[NormalizeKey(institution.Name)])
            .ToList();

        var result = await institutionService.ReorderInstitutionsAsync(
            new ReorderInstitutionCommand(orderedIds),
            cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to restore institution order: {result.Error}");
    }

    private async Task<Dictionary<string, int>> CreateAccountsAsync(
        IReadOnlyList<BackupInstitution> institutions,
        IReadOnlyDictionary<string, int> institutionIds,
        IReadOnlyDictionary<string, int> accountTypeIds,
        CancellationToken cancellationToken)
    {
        var accountIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var institution in institutions)
        {
            var institutionId = institutionIds[NormalizeKey(institution.Name)];

            foreach (var account in institution.Accounts)
            {
                var accountTypeId = accountTypeIds[NormalizeKey(account.AccountType)];
                var result = await accountService.CreateAccountAsync(
                    new CreateAccountCommand(account.Name, institutionId, accountTypeId),
                    cancellationToken);

                if (result.IsFailure || result.Value is null)
                {
                    throw new InvalidOperationException(
                        $"Failed to create account '{account.Name}' at institution '{institution.Name}': {result.Error}");
                }

                accountIds[MakeAccountKey(institution.Name, account.Name)] = result.Value.Id;
            }
        }

        return accountIds;
    }

    // Account.DisplayOrder is a single sequence per user, not per institution, so it is reordered across all institutions at once.
    private async Task ReorderAccountsAsync(
        IReadOnlyList<BackupInstitution> institutions,
        IReadOnlyDictionary<string, int> accountIds,
        CancellationToken cancellationToken)
    {
        var orderedIds = institutions
            .SelectMany(institution => institution.Accounts.Select(account => (institution.Name, account)))
            .OrderBy(entry => entry.account.DisplayOrder)
            .Select(entry => accountIds[MakeAccountKey(entry.Name, entry.account.Name)])
            .ToList();

        var result = await accountService.ReorderAccountsAsync(
            new ReorderAccountCommand(orderedIds),
            cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to restore account order: {result.Error}");
    }

    private async Task CreateFinanceSnapshotsAsync(
        IReadOnlyList<BackupFinanceSnapshot> snapshots,
        IReadOnlyDictionary<string, int> accountIds,
        CancellationToken cancellationToken)
    {
        foreach (var snapshot in snapshots.OrderBy(snapshot => snapshot.Date))
        {
            var balancesByAccountId = snapshot.AccountBalances.ToDictionary(
                balance => accountIds[MakeAccountKey(balance.Institution, balance.Account)],
                balance => balance.Balance);

            var result = await financeSnapshotService.CreateSnapshotAsync(
                new CreateFinanceSnapshotCommand(snapshot.Date, balancesByAccountId),
                cancellationToken);

            if (result.IsFailure)
                throw new InvalidOperationException($"Failed to create snapshot for {snapshot.Date:yyyy-MM-dd}: {result.Error}");
        }

        System.Console.WriteLine($"Created {snapshots.Count} snapshots.");
    }

    private async Task<Dictionary<string, int>> CreateTransactionCategoryGroupsAsync(
        IReadOnlyList<BackupTransactionCategoryGroup> groups,
        CancellationToken cancellationToken)
    {
        var groupIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var result = await transactionCategoryGroupService.CreateTransactionCategoryGroupAsync(
                new CreateTransactionCategoryGroupCommand(group.Name),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
                throw new InvalidOperationException($"Failed to create transaction category group '{group.Name}': {result.Error}");

            groupIds[NormalizeKey(group.Name)] = result.Value.Id;
        }

        return groupIds;
    }

    private async Task ReorderTransactionCategoryGroupsAsync(
        IReadOnlyList<BackupTransactionCategoryGroup> groups,
        IReadOnlyDictionary<string, int> categoryGroupIds,
        CancellationToken cancellationToken)
    {
        var orderedIds = groups
            .OrderBy(group => group.DisplayOrder)
            .Select(group => categoryGroupIds[NormalizeKey(group.Name)])
            .ToList();

        var result = await transactionCategoryGroupService.ReorderTransactionCategoryGroupsAsync(
            new ReorderTransactionCategoryGroupsCommand(orderedIds),
            cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to restore transaction category group order: {result.Error}");
    }

    private async Task<Dictionary<string, int>> CreateTransactionCategoriesAsync(
        IReadOnlyList<BackupTransactionCategoryGroup> groups,
        IReadOnlyDictionary<string, int> categoryGroupIds,
        CancellationToken cancellationToken)
    {
        var categoryIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var groupId = categoryGroupIds[NormalizeKey(group.Name)];

            foreach (var category in group.Categories)
            {
                var result = await transactionCategoryService.CreateTransactionCategoryAsync(
                    new CreateTransactionCategoryCommand(category.Name, groupId),
                    cancellationToken);

                if (result.IsFailure || result.Value is null)
                    throw new InvalidOperationException($"Failed to create transaction category '{category.Name}': {result.Error}");

                categoryIds[NormalizeKey(category.Name)] = result.Value.Id;
            }
        }

        return categoryIds;
    }

    // TransactionCategory.DisplayOrder is a single sequence per user, not per group, so it is reordered across all groups at once.
    private async Task ReorderTransactionCategoriesAsync(
        IReadOnlyList<BackupTransactionCategoryGroup> groups,
        IReadOnlyDictionary<string, int> categoryIds,
        CancellationToken cancellationToken)
    {
        var orderedIds = groups
            .SelectMany(group => group.Categories)
            .OrderBy(category => category.DisplayOrder)
            .Select(category => categoryIds[NormalizeKey(category.Name)])
            .ToList();

        var result = await transactionCategoryService.ReorderTransactionCategoriesAsync(
            new ReorderTransactionCategoriesCommand(orderedIds),
            cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to restore transaction category order: {result.Error}");
    }

    private async Task CreateTransactionsAsync(
        IReadOnlyList<BackupTransaction> transactions,
        IReadOnlyDictionary<string, int> categoryIds,
        IReadOnlyDictionary<string, int> accountIds,
        CancellationToken cancellationToken)
    {
        foreach (var transaction in transactions.OrderBy(t => t.Date))
        {
            var categoryId = categoryIds[NormalizeKey(transaction.Category)];
            var accountId = accountIds[MakeAccountKey(transaction.Institution, transaction.Account)];

            var result = await transactionService.CreateTransactionAsync(
                new CreateTransactionCommand(transaction.Date, transaction.Counterparty, transaction.Amount, categoryId, accountId, transaction.Description),
                cancellationToken);

            if (result.IsFailure)
                throw new InvalidOperationException($"Failed to create transaction dated {transaction.Date:yyyy-MM-dd}: {result.Error}");
        }

        System.Console.WriteLine($"Created {transactions.Count} transactions.");
    }

    private async Task<Dictionary<string, int>> CreateContributionContributorsAsync(
        IReadOnlyList<BackupContributionContributor> contributors,
        CancellationToken cancellationToken)
    {
        var contributorIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var contributor in contributors)
        {
            var result = await contributionContributorService.CreateContributionContributorAsync(
                new CreateContributionContributorCommand(contributor.Name),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
                throw new InvalidOperationException($"Failed to create contribution contributor '{contributor.Name}': {result.Error}");

            contributorIds[NormalizeKey(contributor.Name)] = result.Value.Id;
        }

        return contributorIds;
    }

    private async Task ReorderContributionContributorsAsync(
        IReadOnlyList<BackupContributionContributor> contributors,
        IReadOnlyDictionary<string, int> contributorIds,
        CancellationToken cancellationToken)
    {
        var orderedIds = contributors
            .OrderBy(contributor => contributor.DisplayOrder)
            .Select(contributor => contributorIds[NormalizeKey(contributor.Name)])
            .ToList();

        var result = await contributionContributorService.ReorderContributionContributorsAsync(
            new ReorderContributionContributorsCommand(orderedIds),
            cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to restore contribution contributor order: {result.Error}");
    }

    private async Task CreateContributionsAsync(
        IReadOnlyList<BackupContribution> contributions,
        IReadOnlyDictionary<string, int> contributorIds,
        IReadOnlyDictionary<string, int> accountIds,
        CancellationToken cancellationToken)
    {
        foreach (var contribution in contributions.OrderBy(c => c.Date))
        {
            var contributorId = contributorIds[NormalizeKey(contribution.Contributor)];
            var accountId = accountIds[MakeAccountKey(contribution.Institution, contribution.Account)];

            var result = await contributionService.CreateContributionAsync(
                new CreateContributionCommand(contribution.Date, contribution.Amount, contributorId, accountId),
                cancellationToken);

            if (result.IsFailure)
                throw new InvalidOperationException($"Failed to create contribution dated {contribution.Date:yyyy-MM-dd}: {result.Error}");
        }

        System.Console.WriteLine($"Created {contributions.Count} contributions.");
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
            return null;

        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private static void PrintSummary(BackupDocument document, bool hasExistingData, bool dryRun)
    {
        System.Console.WriteLine(dryRun ? "Dry-run summary" : "Restore summary");
        System.Console.WriteLine($"Backup exported at: {document.ExportedAtUtc:u}");
        System.Console.WriteLine($"Backup username: {document.Username}");
        System.Console.WriteLine($"Existing data for target user: {hasExistingData}");
        System.Console.WriteLine($"Account type groups: {document.AccountTypeGroups.Count}");
        System.Console.WriteLine($"Institutions: {document.Institutions.Count}");
        System.Console.WriteLine($"Snapshots: {document.FinanceSnapshots.Count}");
        System.Console.WriteLine($"Transaction category groups: {document.TransactionCategoryGroups.Count}");
        System.Console.WriteLine($"Transactions: {document.Transactions.Count}");
        System.Console.WriteLine($"Contribution contributors: {document.ContributionContributors.Count}");
        System.Console.WriteLine($"Contributions: {document.Contributions.Count}");
        System.Console.WriteLine();
    }

    private static string MakeAccountKey(string institutionName, string accountName)
        => string.Join('|', NormalizeKey(institutionName), NormalizeKey(accountName));

    private static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();
}
