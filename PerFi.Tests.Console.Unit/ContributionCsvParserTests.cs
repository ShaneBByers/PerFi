using PerFi.Console.Import;
using Xunit;

namespace PerFi.Tests.Console.Unit;

public sealed class ContributionCsvParserTests
{
    [Fact]
    public void Parse_WithValidRows_Succeeds()
    {
        var csv = """
Date,Account Name,Institution,Contributor,Amount
2026-01-09,MiTek 401k,Fidelity,Me,$905.56
2026-01-09,MiTek 401k,Fidelity,Employer,$258.73
""";
        var filePath = CreateTempCsv(csv);
        var parser = new ContributionCsvParser();

        try
        {
            var result = parser.Parse(filePath);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Document!.Rows.Count);
            Assert.Equal(905.56m, result.Document.Rows[0].Amount);
            Assert.Equal("MiTek 401k", result.Document.Rows[0].AccountName);
            Assert.Equal("Fidelity", result.Document.Rows[0].InstitutionName);
            Assert.Equal("Me", result.Document.Rows[0].ContributorName);
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
        var parser = new ContributionCsvParser();

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
Date,Account Name,Bank,Contributor,Amount
2026-01-09,MiTek 401k,Fidelity,Me,$905.56
""";
        var filePath = CreateTempCsv(csv);
        var parser = new ContributionCsvParser();

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
    public void Parse_WithInvalidDate_Fails()
    {
        var csv = """
Date,Account Name,Institution,Contributor,Amount
not-a-date,MiTek 401k,Fidelity,Me,$905.56
""";
        var filePath = CreateTempCsv(csv);
        var parser = new ContributionCsvParser();

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
Date,Account Name,Institution,Contributor,Amount
2026-01-09,MiTek 401k,Fidelity,Me,not-a-number
""";
        var filePath = CreateTempCsv(csv);
        var parser = new ContributionCsvParser();

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
    public void Parse_WithBlankRow_SkipsRow()
    {
        var csv = """
Date,Account Name,Institution,Contributor,Amount
2026-01-09,MiTek 401k,Fidelity,Me,$905.56
,,,,
2026-01-23,MiTek 401k,Fidelity,Me,$905.56
""";
        var filePath = CreateTempCsv(csv);
        var parser = new ContributionCsvParser();

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
        var filePath = Path.Combine(Path.GetTempPath(), $"perfi-contributions-{Guid.NewGuid():N}.csv");
        File.WriteAllText(filePath, csv);
        return filePath;
    }
}
