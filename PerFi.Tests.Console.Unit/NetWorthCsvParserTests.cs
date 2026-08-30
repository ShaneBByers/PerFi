using PerFi.Console.Import;
using Xunit;

namespace PerFi.Tests.Console.Unit;

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
            Assert.Equal("B Bank", result.Document!.Rows[0].InstitutionName);
            Assert.Equal("A Bank", result.Document.Rows[1].InstitutionName);
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

    [Fact]
    public void Parse_WithEmptyFile_Fails()
    {
        var filePath = CreateTempCsv(string.Empty);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("empty", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithMissingRequiredHeader_Fails()
    {
        var csv = """
Bank,Account Type Group,Account Type,Account Name,1/1/2026
Bank A,Investments,IRA,Primary,$1.00
""";
        var filePath = CreateTempCsv(csv);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("must be 'Institution'", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithInvalidDateHeader_Fails()
    {
        var csv = """
Institution,Account Type Group,Account Type,Account Name,NotADate
Bank A,Investments,IRA,Primary,$1.00
""";
        var filePath = CreateTempCsv(csv);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("invalid snapshot date header", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithInvalidBalance_Fails()
    {
        var csv = """
Institution,Account Type Group,Account Type,Account Name,1/1/2026
Bank A,Investments,IRA,Primary,not-a-number
""";
        var filePath = CreateTempCsv(csv);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("invalid balance", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithMissingAccountName_Fails()
    {
        var csv = """
Institution,Account Type Group,Account Type,Account Name,1/1/2026
Bank A,Investments,IRA,,$1.00
""";
        var filePath = CreateTempCsv(csv);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("Account Name", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithSingleValidRow_Succeeds()
    {
        var csv = """
Institution,Account Type Group,Account Type,Account Name,1/1/2026
Bank A,Investments,IRA,Primary,$100.00
""";
        var filePath = CreateTempCsv(csv);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Warnings);
            Assert.Single(result.Document!.Rows);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithUtf8BomInInstitutionHeader_Succeeds()
    {
        var csv = """
__BOM__Institution,Account Type Group,Account Type,Account Name,1/1/2026
Bank A,Investments,IRA,Primary,$100.00
""".Replace("__BOM__", "\uFEFF", StringComparison.Ordinal);
        var filePath = CreateTempCsv(csv);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Document);
            Assert.Single(result.Document!.Rows);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithParenthesesAndTrailingMinusBalances_Succeeds()
    {
        var csv = """
Institution,Account Type Group,Account Type,Account Name,1/1/2026,2/1/2026
Bank A,Loans,Loan,Primary,"($1,234.56)",123.45-
""";
        var filePath = CreateTempCsv(csv);
        var parser = new NetWorthCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.True(result.IsSuccess);
            Assert.Equal(-1234.56m, result.Document!.Rows[0].BalancesByDate[new DateOnly(2026, 1, 1)]);
            Assert.Equal(-123.45m, result.Document.Rows[0].BalancesByDate[new DateOnly(2026, 2, 1)]);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
