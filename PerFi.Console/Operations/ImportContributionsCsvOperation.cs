using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PerFi.Application.Commands;
using PerFi.Console.Import;
using PerFi.Application.Interfaces;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;

namespace PerFi.Console.Operations;

public sealed class ImportContributionsCsvOperation(
    PerFiDbContext dbContext,
    ContributionCsvParser csvParser,
    UserManager<ApplicationUser> userManager,
    ConsoleCurrentUserService currentUser,
    IInstitutionService institutionService,
    IContributionContributorService contributionContributorService,
    IContributionService contributionService)
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

        var hasExistingContributions = (await contributionService.GetAllContributionsAsync(cancellationToken)).Count > 0;

        PrintSummary(importPlan, parseResult.Warnings, hasExistingContributions, dryRun);

        if (hasExistingContributions)
        {
            throw new InvalidOperationException(
                $"Import requires no existing contributions for user '{username}'. Existing contributions were detected.");
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
        System.Console.WriteLine($"Import completed: {importPlan.Rows.Count} contributions imported.");
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
        var contributorIds = await CreateContributionContributorsAsync(importPlan.ContributorNames, cancellationToken);

        foreach (var row in importPlan.Rows)
        {
            var contributorId = contributorIds[NormalizeKey(row.ContributorName)];
            var accountId = importPlan.AccountIdsByRow[row.SourceRowNumber];

            var result = await contributionService.CreateContributionAsync(
                new CreateContributionCommand(row.Date, row.Amount, contributorId, accountId),
                cancellationToken);

            if (result.IsFailure)
            {
                throw new InvalidOperationException($"Failed to import row {row.SourceRowNumber}: {result.Error}");
            }
        }

        System.Console.WriteLine($"Created {importPlan.Rows.Count} contributions.");
    }

    private async Task<Dictionary<string, int>> CreateContributionContributorsAsync(
        IReadOnlyList<string> contributorNames,
        CancellationToken cancellationToken)
    {
        var contributorIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Reuse this user's existing contributor by name instead of creating a duplicate.
        var existingContributors = await contributionContributorService.GetAllContributionContributorsAsync(cancellationToken);
        var existingContributorsByKey = existingContributors.ToDictionary(contributor => NormalizeKey(contributor.Name), contributor => contributor.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var contributorName in contributorNames)
        {
            if (existingContributorsByKey.TryGetValue(NormalizeKey(contributorName), out var existingContributorId))
            {
                contributorIds[NormalizeKey(contributorName)] = existingContributorId;
                continue;
            }

            var result = await contributionContributorService.CreateContributionContributorAsync(
                new CreateContributionContributorCommand(contributorName),
                cancellationToken);

            if (result.IsFailure || result.Value is null)
                throw new InvalidOperationException($"Failed to create contribution contributor '{contributorName}': {result.Error}");

            contributorIds[NormalizeKey(contributorName)] = result.Value.Id;
        }

        return contributorIds;
    }

    private static ImportPlan BuildImportPlan(
        ContributionCsvDocument document,
        IReadOnlyDictionary<string, int> accountIdsByKey)
    {
        var orderedRows = document.Rows.ToArray();

        var contributorNames = orderedRows
            .Select(row => row.ContributorName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var accountIdsByRow = new Dictionary<int, int>();
        var unresolvedAccountErrors = new List<string>();

        foreach (var row in orderedRows)
        {
            var accountKey = MakeAccountKey(row.InstitutionName, row.AccountName);
            if (accountIdsByKey.TryGetValue(accountKey, out var accountId))
            {
                accountIdsByRow[row.SourceRowNumber] = accountId;
            }
            else
            {
                unresolvedAccountErrors.Add(
                    $"Row {row.SourceRowNumber} references unknown account '{row.InstitutionName} / {row.AccountName}'. Run import-net-worth first.");
            }
        }

        return new ImportPlan(orderedRows, contributorNames, accountIdsByRow, unresolvedAccountErrors);
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
        bool hasExistingContributions,
        bool dryRun)
    {
        System.Console.WriteLine(dryRun ? "Dry-run summary" : "Import summary");
        System.Console.WriteLine($"Existing contributions for user: {hasExistingContributions}");
        System.Console.WriteLine($"Contributors to create/reuse: {importPlan.ContributorNames.Count}");
        System.Console.WriteLine($"Contributions to import: {importPlan.Rows.Count}");

        if (warnings.Count > 0)
        {
            System.Console.WriteLine("Warnings:");
            foreach (var warning in warnings)
                System.Console.WriteLine($"- {warning}");
        }

        System.Console.WriteLine("Preview contributions:");
        foreach (var row in importPlan.Rows.Take(5))
        {
            System.Console.WriteLine(
                $"- {row.Date:M/d/yyyy} / {row.AccountName} / {row.ContributorName} / {row.Amount:C}");
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
        IReadOnlyList<ContributionCsvRow> Rows,
        IReadOnlyList<string> ContributorNames,
        IReadOnlyDictionary<int, int> AccountIdsByRow,
        IReadOnlyList<string> UnresolvedAccountErrors);
}
