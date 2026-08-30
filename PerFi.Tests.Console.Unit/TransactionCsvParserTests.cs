using PerFi.Console.Import;
using Xunit;

namespace PerFi.Tests.Console.Unit;

public sealed class TransactionCsvParserTests
{
    [Fact]
    public void Parse_WithValidRows_Succeeds()
    {
        var csv = """
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
2026-01-02,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,"-$1,135.60",
2026-01-09,MiTek,Paycheck,Income,Primary Checking,Chase Primary Checking,Chase,"$3,332.59",
""";
        var filePath = CreateTempCsv(csv);
        var parser = new TransactionCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Document!.Rows.Count);
            Assert.Equal(-1135.60m, result.Document.Rows[0].Amount);
            Assert.Equal("Chase Primary Checking", result.Document.Rows[0].NetWorthAccountName);
            Assert.Equal("Chase", result.Document.Rows[0].NetWorthInstitutionName);
            Assert.Null(result.Document.Rows[0].Description);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithNotes_MapsToDescription()
    {
        var csv = """
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
2026-02-05,Chipotle,Other,Expenses (Optional),Citi Double Cash,Citi Double Cash,Citi,-$74.97,Reimbursed
""";
        var filePath = CreateTempCsv(csv);
        var parser = new TransactionCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.True(result.IsSuccess);
            Assert.Equal("Reimbursed", result.Document!.Rows[0].Description);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithEmptyFile_Fails()
    {
        var filePath = CreateTempCsv(string.Empty);
        var parser = new TransactionCsvParser();

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
Date,Merchant,Category,Category Group,Account,Net Worth Account,Institution,Amount,Notes
2026-01-02,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,-$1.00,
""";
        var filePath = CreateTempCsv(csv);
        var parser = new TransactionCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("must be 'Net Worth Institution'", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithInvalidDate_Fails()
    {
        var csv = """
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
not-a-date,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,-$1.00,
""";
        var filePath = CreateTempCsv(csv);
        var parser = new TransactionCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("invalid date", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithInvalidAmount_Fails()
    {
        var csv = """
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
2026-01-02,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,not-a-number,
""";
        var filePath = CreateTempCsv(csv);
        var parser = new TransactionCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("invalid amount", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithCategoryNameUnderMultipleGroups_Fails()
    {
        var csv = """
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
2026-01-02,Chase Home,Loans,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,-$1.00,
2026-01-03,Chase Home,Loans,Expenses (Optional),Primary Checking,Chase Primary Checking,Chase,-$2.00,
""";
        var filePath = CreateTempCsv(csv);
        var parser = new TransactionCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, error => error.Contains("multiple category groups", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Parse_WithBlankRow_SkipsRow()
    {
        var csv = """
Date,Merchant,Category,Category Group,Account,Net Worth Account,Net Worth Institution,Amount,Notes
2026-01-02,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,-$1.00,
,,,,,,,,
2026-01-03,Chase Home,Mortgage,Expenses (Required),Primary Checking,Chase Primary Checking,Chase,-$2.00,
""";
        var filePath = CreateTempCsv(csv);
        var parser = new TransactionCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Document!.Rows.Count);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTempCsv(string csv)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"perfi-transactions-{Guid.NewGuid():N}.csv");
        File.WriteAllText(filePath, csv);
        return filePath;
    }
}
