using System.Globalization;
using CsvHelper;

namespace PerFi.Console.Import;

public sealed record TransactionCsvRow(
    int SourceRowNumber,
    DateOnly Date,
    string CounterpartyName,
    string CategoryName,
    string CategoryGroupName,
    string NetWorthAccountName,
    string NetWorthInstitutionName,
    decimal Amount,
    string? Description);

public sealed record TransactionCsvDocument(IReadOnlyList<TransactionCsvRow> Rows);

public sealed record TransactionCsvParseResult(
    TransactionCsvDocument? Document,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool IsSuccess => Errors.Count == 0 && Document is not null;
}

public sealed class TransactionCsvParser
{
    private const int MaxNameLength = 200;
    private static readonly string[] SupportedDateFormats = ["yyyy-MM-dd", "M/d/yyyy", "M/d/yy", "MM/dd/yyyy"];

    private static readonly string[] RequiredHeaders =
    [
        "Date",
        "Merchant",
        "Category",
        "Category Group",
        "Account",
        "Net Worth Account",
        "Net Worth Institution",
        "Amount",
        "Notes"
    ];

    public TransactionCsvParseResult Parse(string csvPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);

        var errors = new List<string>();
        var warnings = new List<string>();

        using var streamReader = new StreamReader(csvPath);
        using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);

        if (!csv.Read())
        {
            errors.Add("The CSV file is empty.");
            return new TransactionCsvParseResult(null, errors, warnings);
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];
        ValidateHeaders(headers, errors);

        if (errors.Count > 0)
            return new TransactionCsvParseResult(null, errors, warnings);

        var rows = new List<TransactionCsvRow>();
        var sourceRowNumber = 2;

        while (csv.Read())
        {
            if (IsBlankRow(csv, headers.Length))
            {
                sourceRowNumber++;
                continue;
            }

            var dateField = GetRequiredField(csv, 0, RequiredHeaders[0], sourceRowNumber, errors);
            var merchant = GetRequiredField(csv, 1, RequiredHeaders[1], sourceRowNumber, errors);
            var categoryName = GetRequiredField(csv, 2, RequiredHeaders[2], sourceRowNumber, errors);
            var categoryGroupName = GetRequiredField(csv, 3, RequiredHeaders[3], sourceRowNumber, errors);
            var netWorthAccountName = GetRequiredField(csv, 5, RequiredHeaders[5], sourceRowNumber, errors);
            var netWorthInstitutionName = GetRequiredField(csv, 6, RequiredHeaders[6], sourceRowNumber, errors);
            var amountField = GetRequiredField(csv, 7, RequiredHeaders[7], sourceRowNumber, errors);
            var notes = csv.GetField(8)?.Trim();

            if (!DateOnly.TryParseExact(dateField, SupportedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                errors.Add($"Row {sourceRowNumber} has an invalid date '{dateField}'.");
            }

            if (!CsvCurrencyParser.TryParse(amountField, out var amount))
            {
                errors.Add($"Row {sourceRowNumber} has an invalid amount '{amountField}'.");
            }

            rows.Add(new TransactionCsvRow(
                sourceRowNumber,
                date,
                merchant,
                categoryName,
                categoryGroupName,
                netWorthAccountName,
                netWorthInstitutionName,
                amount,
                string.IsNullOrWhiteSpace(notes) ? null : notes));

            sourceRowNumber++;
        }

        if (rows.Count == 0)
            errors.Add("The CSV file does not contain any transaction rows.");

        ValidateCategoryNames(rows, errors);
        ValidateFieldLengths(rows, errors);

        if (errors.Count > 0)
            return new TransactionCsvParseResult(null, errors, warnings);

        return new TransactionCsvParseResult(new TransactionCsvDocument(rows), errors, warnings);
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

    private static void ValidateCategoryNames(
        IReadOnlyList<TransactionCsvRow> rows,
        ICollection<string> errors)
    {
        foreach (var categoryGroup in rows.GroupBy(row => NormalizeKey(row.CategoryName), StringComparer.OrdinalIgnoreCase))
        {
            var distinctGroups = categoryGroup
                .Select(row => row.CategoryGroupName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(groupName => groupName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (distinctGroups.Length > 1)
            {
                var sample = categoryGroup.First();
                errors.Add(
                    $"Category '{sample.CategoryName}' appears under multiple category groups ({string.Join(", ", distinctGroups)}). The current data model requires category names to be globally unique.");
            }
        }
    }

    private static void ValidateFieldLengths(
        IReadOnlyList<TransactionCsvRow> rows,
        ICollection<string> errors)
    {
        foreach (var row in rows)
        {
            ValidateFieldLength(row.CounterpartyName, "Merchant", row.SourceRowNumber, errors);
            ValidateFieldLength(row.CategoryName, "Category", row.SourceRowNumber, errors);
            ValidateFieldLength(row.CategoryGroupName, "Category Group", row.SourceRowNumber, errors);
            ValidateFieldLength(row.NetWorthAccountName, "Net Worth Account", row.SourceRowNumber, errors);
            ValidateFieldLength(row.NetWorthInstitutionName, "Net Worth Institution", row.SourceRowNumber, errors);
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

    private static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();
}
