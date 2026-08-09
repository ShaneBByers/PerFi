namespace PerFi.Console;

internal sealed record ConsoleCommand(
    string Verb,
    string CsvPath,
    bool DryRun)
{
    public static ConsoleCommand Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new InvalidOperationException(
                "No command was provided. Usage: import-net-worth <csvPath> [--dry-run]");
        }

        var verb = args[0].Trim();

        if (!string.Equals(verb, "import-net-worth", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unknown command '{verb}'. Supported command: import-net-worth <csvPath> [--dry-run]");
        }

        if (args.Length < 2)
        {
            throw new InvalidOperationException(
                "Missing CSV path. Usage: import-net-worth <csvPath> [--dry-run]");
        }

        var csvPath = args[1].Trim();
        var dryRun = args.Skip(2).Any(argument => string.Equals(argument, "--dry-run", StringComparison.OrdinalIgnoreCase));
        var unexpectedArguments = args.Skip(2)
            .Where(argument => !string.Equals(argument, "--dry-run", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (unexpectedArguments.Length > 0)
        {
            throw new InvalidOperationException(
                $"Unexpected arguments: {string.Join(", ", unexpectedArguments)}. Usage: import-net-worth <csvPath> [--dry-run]");
        }

        return new ConsoleCommand(verb, csvPath, dryRun);
    }
}