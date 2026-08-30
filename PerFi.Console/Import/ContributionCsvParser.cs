using System.Globalization;
using CsvHelper;

namespace PerFi.Console.Import;

public sealed record ContributionCsvRow(
    int SourceRowNumber,
    DateOnly Date,
    string AccountName,
    string InstitutionName,
    string ContributorName,
    decimal Amount);

public sealed record ContributionCsvDocument(IReadOnlyList<ContributionCsvRow> Rows);

public sealed record ContributionCsvParseResult(
    ContributionCsvDocument? Document,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool IsSuccess => Errors.Count == 0 && Document is not null;
}

public sealed class ContributionCsvParser
{
    private const int MaxNameLength = 200;
    private static readonly string[] SupportedDateFormats = ["yyyy-MM-dd", "M/d/yyyy", "M/d/yy", "MM/dd/yyyy"];

    private static readonly string[] RequiredHeaders =
    [
        "Date",
        "Account Name",
        "Institution",
        "Contributor",
        "Amount"
    ];

    public ContributionCsvParseResult Parse(string csvPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);

        var errors = new List<string>();
        var warnings = new List<string>();

        using var streamReader = new StreamReader(csvPath);
        using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);

        if (!csv.Read())
        {
            errors.Add("The CSV file is empty.");
            return new ContributionCsvParseResult(null, errors, warnings);
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];
        ValidateHeaders(headers, errors);

        if (errors.Count > 0)
            return new ContributionCsvParseResult(null, errors, warnings);

        var rows = new List<ContributionCsvRow>();
        var sourceRowNumber = 2;

        while (csv.Read())
        {
            if (IsBlankRow(csv, headers.Length))
            {
                sourceRowNumber++;
                continue;
            }

            var dateField = GetRequiredField(csv, 0, RequiredHeaders[0], sourceRowNumber, errors);
            var accountName = GetRequiredField(csv, 1, RequiredHeaders[1], sourceRowNumber, errors);
            var institutionName = GetRequiredField(csv, 2, RequiredHeaders[2], sourceRowNumber, errors);
            var contributorName = GetRequiredField(csv, 3, RequiredHeaders[3], sourceRowNumber, errors);
            var amountField = GetRequiredField(csv, 4, RequiredHeaders[4], sourceRowNumber, errors);

            if (!DateOnly.TryParseExact(dateField, SupportedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                errors.Add($"Row {sourceRowNumber} has an invalid date '{dateField}'.");
            }

            if (!CsvCurrencyParser.TryParse(amountField, out var amount))
            {
                errors.Add($"Row {sourceRowNumber} has an invalid amount '{amountField}'.");
            }

            rows.Add(new ContributionCsvRow(
                sourceRowNumber,
                date,
                accountName,
                institutionName,
                contributorName,
                amount));

            sourceRowNumber++;
        }

        if (rows.Count == 0)
            errors.Add("The CSV file does not contain any contribution rows.");

        ValidateFieldLengths(rows, errors);

        if (errors.Count > 0)
            return new ContributionCsvParseResult(null, errors, warnings);

        return new ContributionCsvParseResult(new ContributionCsvDocument(rows), errors, warnings);
    }

    private static void ValidateHeaders(IReadOnlyList<string> headers, ICollection<string> errors)
    {
        if (headers.Count < RequiredHeaders.Length)
        {
            errors.Add($"The CSV file must contain the columns: {string.Join(", ", RequiredHeaders)}.");
            return;
        }

        for (var index = 0; index < RequiredHeaders.Length; index++)
        {
            var actualHeader = NormalizeHeaderValue(headers[index]);
            if (!string.Equals(actualHeader, RequiredHeaders[index], StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Column {index + 1} must be '{RequiredHeaders[index]}', but was '{actualHeader}'.");
            }
        }
    }

    private static string GetRequiredField(
        CsvReader csv,
        int columnIndex,
        string columnName,
        int sourceRowNumber,
        ICollection<string> errors)
    {
        var value = csv.GetField(columnIndex)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Row {sourceRowNumber} is missing required field '{columnName}'.");
        }

        return value;
    }

    private static bool IsBlankRow(CsvReader csv, int headerCount)
    {
        for (var index = 0; index < headerCount; index++)
        {
            if (!string.IsNullOrWhiteSpace(csv.GetField(index)))
                return false;
        }

        return true;
    }

    private static void ValidateFieldLengths(
        IReadOnlyList<ContributionCsvRow> rows,
        ICollection<string> errors)
    {
        foreach (var row in rows)
        {
            ValidateFieldLength(row.AccountName, "Account Name", row.SourceRowNumber, errors);
            ValidateFieldLength(row.InstitutionName, "Institution", row.SourceRowNumber, errors);
            ValidateFieldLength(row.ContributorName, "Contributor", row.SourceRowNumber, errors);
        }
    }

    private static void ValidateFieldLength(
        string value,
        string fieldName,
        int sourceRowNumber,
        ICollection<string> errors)
    {
        if (value.Length <= MaxNameLength)
            return;

        errors.Add(
            $"Row {sourceRowNumber} field '{fieldName}' exceeds the maximum length of {MaxNameLength} characters.");
    }

    private static string NormalizeHeaderValue(string header)
        => header.Trim().TrimStart('\uFEFF');
}
