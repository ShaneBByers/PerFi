using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PerFi.Console.Backup;
using Xunit;

namespace PerFi.Tests.Console.Integration;

// These tests exercise PerFi.Console/ExampleData/example_backup.json - a fake but structurally complete backup,
// tracked in git (unlike the real, gitignored backup.json), used both as a manual "seed some example data"
// file and as a living contract test: if BackupDocument's schema evolves without updating Import/Export or
// this fixture, the round-trip assertion below fails loudly instead of silently drifting.
public sealed class ImportExampleBackupTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "TestData", "example_backup.json");

    [Fact]
    public void ExampleFixture_SchemaVersion_MatchesCurrentBackupSchemaVersion()
    {
        var document = LoadFixtureDocument();

        Assert.Equal(
            BackupDocument.CurrentSchemaVersion,
            document.SchemaVersion);
    }

    [Fact]
    public async Task ImportAsync_WithExampleFixture_CreatesExpectedData()
    {
        await using var host = await BackupOperationsTestHost.CreateAsync();

        await host.ImportOperation.ExecuteAsync(FixturePath, BackupOperationsTestHost.Username, dryRun: false);

        Assert.Equal(4, await host.DbContext.AccountTypeGroups.CountAsync());
        Assert.Equal(8, await host.DbContext.AccountTypes.CountAsync());
        Assert.Equal(6, await host.DbContext.Institutions.CountAsync());
        Assert.Equal(12, await host.DbContext.Accounts.CountAsync());
        Assert.Equal(48, await host.DbContext.FinanceSnapshots.CountAsync());
        Assert.Equal(576, await host.DbContext.AccountBalances.CountAsync());
        Assert.Equal(6, await host.DbContext.TransactionCategoryGroups.CountAsync());
        Assert.Equal(17, await host.DbContext.TransactionCategories.CountAsync());
        Assert.Equal(72, await host.DbContext.Transactions.CountAsync());
        Assert.Equal(3, await host.DbContext.ContributionContributors.CountAsync());
        Assert.Equal(26, await host.DbContext.Contributions.CountAsync());

        var rentPayments = await host.DbContext.Transactions
            .Where(t => t.CounterpartyName == "Riverside Rentals LLC")
            .ToListAsync();
        Assert.NotEmpty(rentPayments);
        Assert.All(rentPayments, rent =>
        {
            Assert.InRange(rent.Amount, -1650m, -1550m);
            Assert.Equal("Monthly rent", rent.Description);
        });
    }

    [Fact]
    public async Task ImportAsync_WithDryRun_WritesNoData()
    {
        await using var host = await BackupOperationsTestHost.CreateAsync();

        await host.ImportOperation.ExecuteAsync(FixturePath, BackupOperationsTestHost.Username, dryRun: true);

        Assert.Empty(await host.DbContext.AccountTypeGroups.ToListAsync());
        Assert.Empty(await host.DbContext.Institutions.ToListAsync());
    }

    [Fact]
    public async Task ImportAsync_WhenUserAlreadyHasData_Throws()
    {
        await using var host = await BackupOperationsTestHost.CreateAsync();
        await host.ImportOperation.ExecuteAsync(FixturePath, BackupOperationsTestHost.Username, dryRun: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.ImportOperation.ExecuteAsync(FixturePath, BackupOperationsTestHost.Username, dryRun: false));
    }

    [Fact]
    public async Task ImportThenExport_RoundTripsToTheSameDocument()
    {
        await using var host = await BackupOperationsTestHost.CreateAsync();
        await host.ImportOperation.ExecuteAsync(FixturePath, BackupOperationsTestHost.Username, dryRun: false);

        var exportedPath = Path.Combine(Path.GetTempPath(), $"perfi-backup-roundtrip-{Guid.NewGuid():N}.json");
        try
        {
            await host.ExportOperation.ExecuteAsync(exportedPath, BackupOperationsTestHost.Username);

            var original = LoadFixtureDocument();
            var exported = await LoadDocumentAsync(exportedPath);

            // ExportedAtUtc is expected to change on every export; every other field must round-trip exactly.
            Assert.Equal(
                JsonSerializer.Serialize(original with { ExportedAtUtc = default }, BackupJsonOptions.Default),
                JsonSerializer.Serialize(exported with { ExportedAtUtc = default }, BackupJsonOptions.Default));
        }
        finally
        {
            File.Delete(exportedPath);
        }
    }

    private static BackupDocument LoadFixtureDocument()
        => LoadDocumentAsync(FixturePath).GetAwaiter().GetResult();

    private static async Task<BackupDocument> LoadDocumentAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<BackupDocument>(stream, BackupJsonOptions.Default)
            ?? throw new InvalidOperationException($"Backup file '{path}' could not be parsed.");
    }
}
