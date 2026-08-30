using System.Globalization;
using CsvHelper;

namespace PerFi.Console.Import;

public sealed record NetWorthCsvAccountRow(
    int SourceRowNumber,
    string InstitutionName,
    string AccountTypeGroupName,
    string AccountTypeName,
    string AccountName,
    IReadOnlyDictionary<DateOnly, decimal> BalancesByDate);

public sealed record NetWorthCsvDocument(
    IReadOnlyList<NetWorthCsvAccountRow> Rows,
    IReadOnlyList<DateOnly> SourceSnapshotDates,
    IReadOnlyList<DateOnly> SortedSnapshotDates);

public sealed record NetWorthCsvParseResult(
    NetWorthCsvDocument? Document,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool IsSuccess => Errors.Count == 0 && Document is not null;
}

public sealed class NetWorthCsvParser
{
    private const int MaxNameLength = 200;
    private static readonly string[] SupportedDateFormats = ["M/d/yyyy", "M/d/yy", "MM/dd/yyyy", "yyyy-MM-dd"];

    private static readonly string[] RequiredHeaders =
    [
        "Institution",
        "Account Type Group",
        "Account Type",
        "Account Name"
    ];

    public NetWorthCsvParseResult Parse(string csvPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);

        var errors = new List<string>();
        var warnings = new List<string>();

        using var streamReader = new StreamReader(csvPath);
        using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);

        if (!csv.Read())
        {
            errors.Add("The CSV file is empty.");
            return new NetWorthCsvParseResult(null, errors, warnings);
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];
        var dateColumns = ParseHeaders(headers, errors, warnings);

        if (errors.Count > 0)
            return new NetWorthCsvParseResult(null, errors, warnings);

        var rows = new List<NetWorthCsvAccountRow>();
        var sourceRowNumber = 2;

        while (csv.Read())
        {
            if (IsBlankRow(csv, headers.Length))
            {
                sourceRowNumber++;
                continue;
            }

            var institutionName = GetRequiredField(csv, 0, RequiredHeaders[0], sourceRowNumber, errors);
            var accountTypeGroupName = GetRequiredField(csv, 1, RequiredHeaders[1], sourceRowNumber, errors);
            var accountTypeName = GetRequiredField(csv, 2, RequiredHeaders[2], sourceRowNumber, errors);
            var accountName = GetRequiredField(csv, 3, RequiredHeaders[3], sourceRowNumber, errors);

            var balancesByDate = new Dictionary<DateOnly, decimal>();

            foreach (var dateColumn in dateColumns)
            {
                var rawValue = csv.GetField(dateColumn.ColumnIndex);
                if (string.IsNullOrWhiteSpace(rawValue))
                    continue;

                if (!CsvCurrencyParser.TryParse(rawValue, out var balance))
                {
                    errors.Add(
                        $"Row {sourceRowNumber} has an invalid balance '{rawValue}' for snapshot date {dateColumn.Date:M/d/yyyy}.");
                    continue;
                }

                balancesByDate[dateColumn.Date] = balance;
            }

            if (balancesByDate.Count == 0)
            {
                errors.Add($"Row {sourceRowNumber} does not contain any usable snapshot balances.");
            }

            rows.Add(new NetWorthCsvAccountRow(
                sourceRowNumber,
                institutionName,
                accountTypeGroupName,
                accountTypeName,
                accountName,
                balancesByDate));

            sourceRowNumber++;
        }

        if (rows.Count == 0)
            errors.Add("The CSV file does not contain any account rows.");

        ValidateDuplicateLogicalAccounts(rows, errors);
        ValidateAccountTypeNames(rows, errors);
        ValidateFieldLengths(rows, errors);

        if (errors.Count > 0)
            return new NetWorthCsvParseResult(null, errors, warnings);

        var sourceDates = dateColumns.Select(column => column.Date).ToArray();
        var sortedDates = sourceDates.OrderBy(date => date).ToArray();
        var document = new NetWorthCsvDocument(rows, sourceDates, sortedDates);

        return new NetWorthCsvParseResult(document, errors, warnings);
    }

    private static IReadOnlyList<DateColumn> ParseHeaders(
        IReadOnlyList<string> headers,
        ICollection<string> errors,
        ICollection<string> warnings)
    {
        if (headers.Count < RequiredHeaders.Length + 1)
        {
            errors.Add("The CSV file must contain the four metadata columns plus at least one snapshot date column.");
            return [];
        }

        for (var index = 0; index < RequiredHeaders.Length; index++)
        {
            var actualHeader = NormalizeHeaderValue(headers[index]);
            if (!string.Equals(actualHeader, RequiredHeaders[index], StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"Column {index + 1} must be '{RequiredHeaders[index]}', but was '{actualHeader}'.");
            }
        }

        var dateColumns = new List<DateColumn>();
        var seenDates = new HashSet<DateOnly>();

        for (var index = RequiredHeaders.Length; index < headers.Count; index++)
        {
            var rawHeader = NormalizeHeaderValue(headers[index]);
            if (string.IsNullOrWhiteSpace(rawHeader))
            {
                errors.Add($"Column {index + 1} has an empty snapshot date header.");
                continue;
            }

            if (!DateOnly.TryParseExact(rawHeader, SupportedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var snapshotDate))
            {
                errors.Add($"Column {index + 1} has an invalid snapshot date header '{rawHeader}'.");
                continue;
            }

            if (!seenDates.Add(snapshotDate))
            {
                errors.Add($"The snapshot date '{snapshotDate:M/d/yyyy}' appears more than once in the header row.");
                continue;
            }

            dateColumns.Add(new DateColumn(index, snapshotDate));
        }

        if (dateColumns.Count > 1)
        {
            var sourceDates = dateColumns.Select(column => column.Date).ToArray();
            var sortedDates = sourceDates.OrderBy(date => date).ToArray();

            if (!sourceDates.SequenceEqual(sortedDates))
            {
                warnings.Add("Snapshot date columns are not in ascending order; they will be imported in ascending order.");
            }
        }

        return dateColumns;
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

    private static void ValidateDuplicateLogicalAccounts(
        IReadOnlyList<NetWorthCsvAccountRow> rows,
        ICollection<string> errors)
    {
        foreach (var duplicateGroup in rows.GroupBy(
                     row => MakeAccountKey(row.InstitutionName, row.AccountTypeGroupName, row.AccountTypeName, row.AccountName),
                     StringComparer.OrdinalIgnoreCase)
                 .Where(group => group.Count() > 1))
        {
            var sample = duplicateGroup.First();
            var rowNumbers = string.Join(", ", duplicateGroup.Select(row => row.SourceRowNumber));
            errors.Add(
                $"Duplicate logical account '{sample.InstitutionName} / {sample.AccountTypeGroupName} / {sample.AccountTypeName} / {sample.AccountName}' found on rows {rowNumbers}.");
        }
    }

    private static void ValidateAccountTypeNames(
        IReadOnlyList<NetWorthCsvAccountRow> rows,
        ICollection<string> errors)
    {
        foreach (var accountTypeGroup in rows.GroupBy(
                     row => NormalizeKey(row.AccountTypeName),
                     StringComparer.OrdinalIgnoreCase))
        {
            var distinctGroups = accountTypeGroup
                .Select(row => row.AccountTypeGroupName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(groupName => groupName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (distinctGroups.Length > 1)
            {
                var sample = accountTypeGroup.First();
                errors.Add(
                    $"Account type '{sample.AccountTypeName}' appears under multiple account type groups ({string.Join(", ", distinctGroups)}). The current data model requires account type names to be globally unique.");
            }
        }
    }

    private static void ValidateFieldLengths(
        IReadOnlyList<NetWorthCsvAccountRow> rows,
        ICollection<string> errors)
    {
        foreach (var row in rows)
        {
            ValidateFieldLength(row.InstitutionName, "Institution", row.SourceRowNumber, errors);
            ValidateFieldLength(row.AccountTypeGroupName, "Account Type Group", row.SourceRowNumber, errors);
            ValidateFieldLength(row.AccountTypeName, "Account Type", row.SourceRowNumber, errors);
            ValidateFieldLength(row.AccountName, "Account Name", row.SourceRowNumber, errors);
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

    private static string MakeAccountKey(
        string institutionName,
        string accountTypeGroupName,
        string accountTypeName,
        string accountName)
        => string.Join('|',
            NormalizeKey(institutionName),
            NormalizeKey(accountTypeGroupName),
            NormalizeKey(accountTypeName),
            NormalizeKey(accountName));

    private static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();

    private sealed record DateColumn(int ColumnIndex, DateOnly Date);
}