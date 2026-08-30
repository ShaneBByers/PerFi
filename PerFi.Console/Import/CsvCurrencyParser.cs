using System.Globalization;

namespace PerFi.Console.Import;

internal static class CsvCurrencyParser
{
    public static bool TryParse(string rawValue, out decimal amount)
    {
        var sanitizedValue = rawValue.Trim();
        var isNegative = false;

        if (sanitizedValue.StartsWith('(') && sanitizedValue.EndsWith(')'))
        {
            sanitizedValue = sanitizedValue[1..^1];
            isNegative = true;
        }

        if (sanitizedValue.EndsWith('-'))
        {
            sanitizedValue = sanitizedValue[..^1];
            isNegative = true;
        }

        sanitizedValue = sanitizedValue
            .Replace("$", string.Empty)
            .Replace(",", string.Empty)
            .Replace(" ", string.Empty);

        var isParsed = decimal.TryParse(
            sanitizedValue,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out amount);

        if (!isParsed)
            return false;

        if (isNegative && amount > 0)
            amount = -amount;

        return true;
    }
}
