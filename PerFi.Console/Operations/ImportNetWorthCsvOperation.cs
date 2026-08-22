using Microsoft.EntityFrameworkCore.Storage;
using PerFi.Application.Commands;
using Microsoft.EntityFrameworkCore;
using PerFi.Console.Import;
using PerFi.Application.Interfaces;
using PerFi.Infrastructure;

namespace PerFi.Console.Operations;

public sealed class ImportNetWorthCsvOperation(
    PerFiDbContext dbContext,
    NetWorthCsvParser csvParser,
    IInstitutionService institutionService,
    IAccountTypeGroupService accountTypeGroupService,
    IAccountTypeService accountTypeService,
    IAccountService accountService,
    IFinanceSnapshotService financeSnapshotService)
{
    public async Task ExecuteAsync(string csvPath, bool dryRun, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);

        var resolvedPath = Path.GetFullPath(csvPath);

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"CSV file '{resolvedPath}' was not found.", resolvedPath);
        }

        System.Console.WriteLine($"Import path: {resolvedPath}");
        System.Console.WriteLine($"Mode: {(dryRun ? "dry-run" : "import")}");
        System.Console.WriteLine();

        var parseResult = csvParser.Parse(resolvedPath);
        if (!parseResult.IsSuccess)
            throw new InvalidOperationException(FormatValidationFailure(parseResult.Errors, parseResult.Warnings));

        var document = parseResult.Document!;
        var importPlan = BuildImportPlan(document);
        var hasExistingData = await DatabaseHasExistingDataAsync(cancellationToken);

        PrintSummary(importPlan, parseResult.Warnings, hasExistingData, dryRun);

        if (hasExistingData)
        {
            throw new InvalidOperationException(
                "Import requires an empty database. Existing institutions, account metadata, accounts, or snapshots were detected.");
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
        System.Console.WriteLine(
            $"Import completed: {importPlan.Accounts.Count} accounts across {importPlan.SnapshotBalancesByDate.Count} snapshots from {importPlan.FirstSnapshotDate:M/d/yyyy} to {importPlan.LastSnapshotDate:M/d/yyyy}.");
    }

    public async Task<bool> DatabaseHasExistingDataAsync(CancellationToken cancellationToken = default)
        => await dbContext.Institutions.AnyAsync(cancellationToken)
            || await dbContext.AccountTypeGroups.AnyAsync(cancellationToken)
            || await dbContext.AccountTypes.AnyAsync(cancellationToken)
            || await dbContext.Accounts.AnyAsync(cancellationToken)
            || await dbContext.FinanceSnapshots.AnyAsync(cancellationToken);

    private async Task ImportAsync(ImportPlan importPlan, CancellationToken cancellationToken)
    {
        var accountTypeGroupIds = await CreateAccountTypeGroupsAsync(importPlan.AccountTypeGroups, cancellationToken);
        var accountTypeIds = await CreateAccountTypesAsync(importPlan.AccountTypes, accountTypeGroupIds, cancellationToken);
        var institutionIds = await CreateInstitutionsAsync(importPlan.Institutions, cancellationToken);
        var accountIds = await CreateAccountsAsync(importPlan.Accounts, institutionIds, accountTypeIds, cancellationToken);

        foreach (var snapshot in importPlan.SnapshotBalancesByDate)
        {
            var balancesByAccountId = new Dictionary<int, decimal>();

            foreach (var accountBalance in snapshot.Value)
            {
                var accountKey = MakeAccountKey(
                    accountBalance.InstitutionName,
                    accountBalance.AccountTypeGroupName,
                    accountBalance.AccountTypeName,
                    accountBalance.AccountName);
                balancesByAccountId[accountIds[accountKey]] = accountBalance.Balance;
            }

            var result = await financeSnapshotService.CreateSnapshotAsync(
                new CreateFinanceSnapshotCommand(snapshot.Key, balancesByAccountId),
                cancellationToken);

            if (result.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Failed to create snapshot for {snapshot.Key:M/d/yyyy}: {result.Error}");
            }

            System.Console.WriteLine(
                $"Created snapshot for {snapshot.Key:M/d/yyyy} with {balancesByAccountId.Count} account balances.");
        }
    }

    private async Task<Dictionary<string, int>> CreateAccountTypeGroupsAsync(
        IReadOnlyList<string> groupNames,
        CancellationToken cancellationToken)
    {
        var groupIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupName in groupNames)
        {
            var result = await accountTypeGroupService.CreateAccountTypeGroupAsync(
                new CreateAccountTypeGroupCommand(groupName),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
                throw new InvalidOperationException($"Failed to create account type group '{groupName}': {result.Error}");

            groupIds[NormalizeKey(groupName)] = result.Value.Id;
        }

        return groupIds;
    }

    private async Task<Dictionary<string, int>> CreateAccountTypesAsync(
        IReadOnlyList<AccountTypeSeed> accountTypes,
        IReadOnlyDictionary<string, int> accountTypeGroupIds,
        CancellationToken cancellationToken)
    {
        var accountTypeIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var accountType in accountTypes)
        {
            var groupId = accountTypeGroupIds[NormalizeKey(accountType.GroupName)];
            var result = await accountTypeService.CreateAccountTypeAsync(
                new CreateAccountTypeCommand(accountType.TypeName, groupId),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
            {
                throw new InvalidOperationException(
                    $"Failed to create account type '{accountType.TypeName}': {result.Error}");
            }

            accountTypeIds[MakeAccountTypeKey(accountType.GroupName, accountType.TypeName)] = result.Value.Id;
        }

        return accountTypeIds;
    }

    private async Task<Dictionary<string, int>> CreateInstitutionsAsync(
        IReadOnlyList<string> institutionNames,
        CancellationToken cancellationToken)
    {
        var institutionIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var institutionName in institutionNames)
        {
            var result = await institutionService.CreateInstitutionAsync(
                new CreateInstitutionCommand(institutionName),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
                throw new InvalidOperationException($"Failed to create institution '{institutionName}': {result.Error}");

            institutionIds[NormalizeKey(institutionName)] = result.Value.Id;
        }

        return institutionIds;
    }

    private async Task<Dictionary<string, int>> CreateAccountsAsync(
        IReadOnlyList<AccountSeed> accounts,
        IReadOnlyDictionary<string, int> institutionIds,
        IReadOnlyDictionary<string, int> accountTypeIds,
        CancellationToken cancellationToken)
    {
        var accountIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var account in accounts)
        {
            var institutionId = institutionIds[NormalizeKey(account.InstitutionName)];
            var accountTypeId = accountTypeIds[MakeAccountTypeKey(account.AccountTypeGroupName, account.AccountTypeName)];
            var result = await accountService.CreateAccountAsync(
                new CreateAccountCommand(account.AccountName, institutionId, accountTypeId),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
            {
                throw new InvalidOperationException(
                    $"Failed to create account '{account.AccountName}' at institution '{account.InstitutionName}': {result.Error}");
            }

            accountIds[MakeAccountKey(account.InstitutionName, account.AccountTypeGroupName, account.AccountTypeName, account.AccountName)] = result.Value.Id;
        }

        return accountIds;
    }

    private static ImportPlan BuildImportPlan(NetWorthCsvDocument document)
    {
        var orderedRows = document.Rows.ToArray();

        var accountTypeGroups = orderedRows
            .Select(row => row.AccountTypeGroupName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var accountTypes = orderedRows
            .GroupBy(row => MakeAccountTypeKey(row.AccountTypeGroupName, row.AccountTypeName), StringComparer.OrdinalIgnoreCase)
            .Select(group => new AccountTypeSeed(group.First().AccountTypeGroupName, group.First().AccountTypeName))
            .ToArray();

        var institutions = orderedRows
            .Select(row => row.InstitutionName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var accounts = orderedRows
            .Select(row => new AccountSeed(
                row.InstitutionName,
                row.AccountTypeGroupName,
                row.AccountTypeName,
                row.AccountName))
            .ToArray();

        var snapshotBalancesByDate = new SortedDictionary<DateOnly, IReadOnlyList<AccountBalanceSeed>>();

        foreach (var snapshotDate in document.SortedSnapshotDates)
        {
            var balances = orderedRows
                .Where(row => row.BalancesByDate.ContainsKey(snapshotDate))
                .Select(row => new AccountBalanceSeed(
                    row.InstitutionName,
                    row.AccountTypeGroupName,
                    row.AccountTypeName,
                    row.AccountName,
                    row.BalancesByDate[snapshotDate]))
                .ToArray();

            if (balances.Length > 0)
                snapshotBalancesByDate[snapshotDate] = balances;
        }

        var totalBalanceCount = snapshotBalancesByDate.Sum(snapshot => snapshot.Value.Count);

        return new ImportPlan(
            accountTypeGroups,
            accountTypes,
            institutions,
            accounts,
            snapshotBalancesByDate,
            document.SortedSnapshotDates[0],
            document.SortedSnapshotDates[^1],
            totalBalanceCount);
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
        bool hasExistingData,
        bool dryRun)
    {
        System.Console.WriteLine(dryRun ? "Dry-run summary" : "Import summary");
        System.Console.WriteLine($"Database empty: {!hasExistingData}");
        System.Console.WriteLine($"Account type groups to create: {importPlan.AccountTypeGroups.Count}");
        System.Console.WriteLine($"Account types to create: {importPlan.AccountTypes.Count}");
        System.Console.WriteLine($"Institutions to create: {importPlan.Institutions.Count}");
        System.Console.WriteLine($"Accounts to create: {importPlan.Accounts.Count}");
        System.Console.WriteLine($"Snapshots to create: {importPlan.SnapshotBalancesByDate.Count}");
        System.Console.WriteLine($"Account balances to import: {importPlan.TotalBalanceCount}");
        System.Console.WriteLine(
            $"Snapshot range: {importPlan.FirstSnapshotDate:M/d/yyyy} -> {importPlan.LastSnapshotDate:M/d/yyyy}");

        if (warnings.Count > 0)
        {
            System.Console.WriteLine("Warnings:");
            foreach (var warning in warnings)
                System.Console.WriteLine($"- {warning}");
        }

        System.Console.WriteLine("Preview institutions:");
        foreach (var institutionName in importPlan.Institutions.Take(5))
            System.Console.WriteLine($"- {institutionName}");

        System.Console.WriteLine("Preview accounts:");
        foreach (var account in importPlan.Accounts.Take(5))
        {
            System.Console.WriteLine(
                $"- {account.InstitutionName} / {account.AccountTypeGroupName} / {account.AccountTypeName} / {account.AccountName}");
        }

        System.Console.WriteLine("Preview snapshot dates:");
        foreach (var snapshotDate in importPlan.SnapshotBalancesByDate.Keys.Take(5))
            System.Console.WriteLine($"- {snapshotDate:M/d/yyyy}");

        System.Console.WriteLine();
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
            return null;

        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private static string MakeAccountTypeKey(string groupName, string typeName)
        => string.Join('|', NormalizeKey(groupName), NormalizeKey(typeName));

    private static string MakeAccountKey(
        string institutionName,
        string groupName,
        string typeName,
        string accountName)
        => string.Join('|', NormalizeKey(institutionName), NormalizeKey(groupName), NormalizeKey(typeName), NormalizeKey(accountName));

    private static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();

    private sealed record ImportPlan(
        IReadOnlyList<string> AccountTypeGroups,
        IReadOnlyList<AccountTypeSeed> AccountTypes,
        IReadOnlyList<string> Institutions,
        IReadOnlyList<AccountSeed> Accounts,
        IReadOnlyDictionary<DateOnly, IReadOnlyList<AccountBalanceSeed>> SnapshotBalancesByDate,
        DateOnly FirstSnapshotDate,
        DateOnly LastSnapshotDate,
        int TotalBalanceCount);

    private sealed record AccountTypeSeed(string GroupName, string TypeName);

    private sealed record AccountSeed(
        string InstitutionName,
        string AccountTypeGroupName,
        string AccountTypeName,
        string AccountName);

    private sealed record AccountBalanceSeed(
        string InstitutionName,
        string AccountTypeGroupName,
        string AccountTypeName,
        string AccountName,
        decimal Balance);
}