using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PerFi.Application.Commands;
using PerFi.Console.Import;
using PerFi.Application.Interfaces;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;

namespace PerFi.Console.Operations;

public sealed class ImportTransactionsCsvOperation(
    PerFiDbContext dbContext,
    TransactionCsvParser csvParser,
    UserManager<ApplicationUser> userManager,
    ConsoleCurrentUserService currentUser,
    IInstitutionService institutionService,
    ITransactionCategoryGroupService transactionCategoryGroupService,
    ITransactionCategoryService transactionCategoryService,
    ITransactionService transactionService)
{
    public async Task ExecuteAsync(string csvPath, string username, bool dryRun, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var user = await userManager.FindByNameAsync(username);
        if (user is null)
            throw new InvalidOperationException($"User '{username}' was not found. Run 'create-user {username} <password>' first.");

        currentUser.UserId = user.Id;

        var resolvedPath = Path.GetFullPath(csvPath);

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"CSV file '{resolvedPath}' was not found.", resolvedPath);
        }

        System.Console.WriteLine($"Import path: {resolvedPath}");
        System.Console.WriteLine($"Importing for user: {username}");
        System.Console.WriteLine($"Mode: {(dryRun ? "dry-run" : "import")}");
        System.Console.WriteLine();

        var parseResult = csvParser.Parse(resolvedPath);
        if (!parseResult.IsSuccess)
            throw new InvalidOperationException(FormatValidationFailure(parseResult.Errors, parseResult.Warnings));

        var document = parseResult.Document!;
        var accountIdsByKey = await BuildAccountLookupAsync(cancellationToken);
        var importPlan = BuildImportPlan(document, accountIdsByKey);

        if (importPlan.UnresolvedAccountErrors.Count > 0)
        {
            throw new InvalidOperationException(FormatValidationFailure(importPlan.UnresolvedAccountErrors, []));
        }

        var hasExistingTransactions = (await transactionService.GetAllTransactionsAsync(cancellationToken)).Count > 0;

        PrintSummary(importPlan, parseResult.Warnings, hasExistingTransactions, dryRun);

        if (hasExistingTransactions)
        {
            throw new InvalidOperationException(
                $"Import requires no existing transactions for user '{username}'. Existing transactions were detected.");
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
            await ImportAsync(importPlan, cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
        });

        System.Console.WriteLine();
        System.Console.WriteLine($"Import completed: {importPlan.Rows.Count} transactions imported.");
    }

    private async Task<Dictionary<string, int>> BuildAccountLookupAsync(CancellationToken cancellationToken)
    {
        var institutions = await institutionService.GetAllInstitutionsAsync(cancellationToken);
        var accountIdsByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var institution in institutions)
        {
            foreach (var account in institution.Accounts)
            {
                accountIdsByKey[MakeAccountKey(institution.Name, account.Name)] = account.Id;
            }
        }

        return accountIdsByKey;
    }

    private async Task ImportAsync(ImportPlan importPlan, CancellationToken cancellationToken)
    {
        var categoryGroupIds = await CreateTransactionCategoryGroupsAsync(importPlan.CategoryGroupNames, cancellationToken);
        var categoryIds = await CreateTransactionCategoriesAsync(importPlan.Categories, categoryGroupIds, cancellationToken);

        foreach (var row in importPlan.Rows)
        {
            var categoryId = categoryIds[NormalizeKey(row.CategoryName)];
            var accountId = importPlan.AccountIdsByRow[row.SourceRowNumber];

            var result = await transactionService.CreateTransactionAsync(
                new CreateTransactionCommand(row.Date, row.CounterpartyName, row.Amount, categoryId, accountId, row.Description),
                cancellationToken);

            if (result.IsFailure)
            {
                throw new InvalidOperationException($"Failed to import row {row.SourceRowNumber}: {result.Error}");
            }
        }

        System.Console.WriteLine($"Created {importPlan.Rows.Count} transactions.");
    }

    private async Task<Dictionary<string, int>> CreateTransactionCategoryGroupsAsync(
        IReadOnlyList<string> groupNames,
        CancellationToken cancellationToken)
    {
        var groupIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Reuse this user's existing category group by name instead of creating a duplicate.
        var existingGroups = await transactionCategoryGroupService.GetAllTransactionCategoryGroupsAsync(cancellationToken);
        var existingGroupsByKey = existingGroups.ToDictionary(group => NormalizeKey(group.Name), group => group.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var groupName in groupNames)
        {
            if (existingGroupsByKey.TryGetValue(NormalizeKey(groupName), out var existingGroupId))
            {
                groupIds[NormalizeKey(groupName)] = existingGroupId;
                continue;
            }

            var result = await transactionCategoryGroupService.CreateTransactionCategoryGroupAsync(
                new CreateTransactionCategoryGroupCommand(groupName),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
                throw new InvalidOperationException($"Failed to create transaction category group '{groupName}': {result.Error}");

            groupIds[NormalizeKey(groupName)] = result.Value.Id;
        }

        return groupIds;
    }

    private async Task<Dictionary<string, int>> CreateTransactionCategoriesAsync(
        IReadOnlyList<CategorySeed> categories,
        IReadOnlyDictionary<string, int> categoryGroupIds,
        CancellationToken cancellationToken)
    {
        var categoryIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Reuse this user's existing category by name instead of creating a duplicate.
        // Category names are globally unique per user in the repository layer.
        var existingCategories = await transactionCategoryService.GetAllTransactionCategoriesAsync(cancellationToken);
        var existingCategoriesByName = existingCategories
            .GroupBy(category => NormalizeKey(category.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories)
        {
            var normalizedCategoryName = NormalizeKey(category.CategoryName);

            if (existingCategoriesByName.TryGetValue(normalizedCategoryName, out var existingCategoryId))
            {
                categoryIds[normalizedCategoryName] = existingCategoryId;
                continue;
            }

            var groupId = categoryGroupIds[NormalizeKey(category.GroupName)];
            var result = await transactionCategoryService.CreateTransactionCategoryAsync(
                new CreateTransactionCategoryCommand(category.CategoryName, groupId),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
            {
                throw new InvalidOperationException(
                    $"Failed to create transaction category '{category.CategoryName}': {result.Error}");
            }

            categoryIds[normalizedCategoryName] = result.Value.Id;
        }

        return categoryIds;
    }

    private static ImportPlan BuildImportPlan(
        TransactionCsvDocument document,
        IReadOnlyDictionary<string, int> accountIdsByKey)
    {
        var orderedRows = document.Rows.ToArray();

        var categoryGroupNames = orderedRows
            .Select(row => row.CategoryGroupName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var categories = orderedRows
            .GroupBy(row => NormalizeKey(row.CategoryName), StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategorySeed(group.First().CategoryGroupName, group.First().CategoryName))
            .ToArray();

        var accountIdsByRow = new Dictionary<int, int>();
        var unresolvedAccountErrors = new List<string>();

        foreach (var row in orderedRows)
        {
            var accountKey = MakeAccountKey(row.NetWorthInstitutionName, row.NetWorthAccountName);
            if (accountIdsByKey.TryGetValue(accountKey, out var accountId))
            {
                accountIdsByRow[row.SourceRowNumber] = accountId;
            }
            else
            {
                unresolvedAccountErrors.Add(
                    $"Row {row.SourceRowNumber} references unknown account '{row.NetWorthInstitutionName} / {row.NetWorthAccountName}'. Run import-net-worth first.");
            }
        }

        return new ImportPlan(orderedRows, categoryGroupNames, categories, accountIdsByRow, unresolvedAccountErrors);
    }

    private static string FormatValidationFailure(
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings)
    {
        var lines = new List<string> { "CSV validation failed:" };
        lines.AddRange(errors.Select(error => $"- {error}"));

        if (warnings.Count > 0)
        {
            lines.Add("Warnings:");
            lines.AddRange(warnings.Select(warning => $"- {warning}"));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void PrintSummary(
        ImportPlan importPlan,
        IReadOnlyList<string> warnings,
        bool hasExistingTransactions,
        bool dryRun)
    {
        System.Console.WriteLine(dryRun ? "Dry-run summary" : "Import summary");
        System.Console.WriteLine($"Existing transactions for user: {hasExistingTransactions}");
        System.Console.WriteLine($"Category groups to create/reuse: {importPlan.CategoryGroupNames.Count}");
        System.Console.WriteLine($"Categories to create/reuse: {importPlan.Categories.Count}");
        System.Console.WriteLine($"Transactions to import: {importPlan.Rows.Count}");

        if (warnings.Count > 0)
        {
            System.Console.WriteLine("Warnings:");
            foreach (var warning in warnings)
                System.Console.WriteLine($"- {warning}");
        }

        System.Console.WriteLine("Preview transactions:");
        foreach (var row in importPlan.Rows.Take(5))
        {
            System.Console.WriteLine(
                $"- {row.Date:M/d/yyyy} / {row.CounterpartyName} / {row.CategoryName} / {row.Amount:C}");
        }

        System.Console.WriteLine();
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
            return null;

        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private static string MakeAccountKey(string institutionName, string accountName)
        => string.Join('|', NormalizeKey(institutionName), NormalizeKey(accountName));

    private static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();

    private sealed record ImportPlan(
        IReadOnlyList<TransactionCsvRow> Rows,
        IReadOnlyList<string> CategoryGroupNames,
        IReadOnlyList<CategorySeed> Categories,
        IReadOnlyDictionary<int, int> AccountIdsByRow,
        IReadOnlyList<string> UnresolvedAccountErrors);

    private sealed record CategorySeed(string GroupName, string CategoryName);
}
