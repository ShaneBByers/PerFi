using PerFi.Console.Import;
using Xunit;

namespace PerFi.Tests.Console;

public sealed class NetWorthCsvParserTests
{
    [Fact]
    public void Parse_WithOutOfOrderDatesAndRows_SucceedsAndSortsDates()
    {
        var csv = """
Institution,Account Type Group,Account Type,Account Name,2/1/2026,1/1/2026
B Bank,Investments,IRA,Zeta,$2.00,$1.00
A Bank,Checking & Savings,Checking,Alpha,-$3.00,$4.00
""";
        var filePath = CreateTempCsv(csv);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Document);
            Assert.Single(result.Warnings);
            Assert.Equal(new DateOnly(2026, 1, 1), result.Document!.SortedSnapshotDates[0]);
            Assert.Equal(new DateOnly(2026, 2, 1), result.Document.SortedSnapshotDates[1]);
            Assert.Equal(-3.00m, result.Document.Rows[1].BalancesByDate[new DateOnly(2026, 2, 1)]);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithDuplicateLogicalAccount_Fails()
    {
        var csv = """
Institution,Account Type Group,Account Type,Account Name,1/1/2026
Bank A,Investments,IRA,Primary,$1.00
Bank A,Investments,IRA,Primary,$2.00
""";
        var filePath = CreateTempCsv(csv);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("Duplicate logical account", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTempCsv(string csv)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"perfi-net-worth-{Guid.NewGuid():N}.csv");
        File.WriteAllText(filePath, csv);
        return filePath;
    }
}