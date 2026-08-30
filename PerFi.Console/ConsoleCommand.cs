namespace PerFi.Console;

internal sealed record ConsoleCommand(
    string Verb,
    string? CsvPath = null,
    string? Username = null,
    string? Password = null,
    bool DryRun = false,
    bool SkipConfirmation = false)
{
    private const string Usage =
        "Usage: export-backup <backupPath> --username <username> | import-backup <backupPath> --username <username> [--dry-run] | create-user <username> <password> | reset-database [--yes]";

    public static ConsoleCommand Parse(string[] args)
    {
        if (args.Length == 0)
            throw new InvalidOperationException($"No command was provided. {Usage}");

        var verb = args[0].Trim();

        return verb.ToLowerInvariant() switch
        {
            "export-backup" => ParseExportBackup(args),
            "import-backup" => ParseImportBackup(args),
            "create-user" => ParseCreateUser(args),
            "reset-database" => ParseResetDatabase(args),
            _ => throw new InvalidOperationException($"Unknown command '{verb}'. {Usage}")
        };
    }

    private static ConsoleCommand ParseCreateUser(string[] args)
    {
        if (args.Length < 3)
            throw new InvalidOperationException($"Missing username or password. {Usage}");

        if (args.Length > 3)
            throw new InvalidOperationException($"Unexpected arguments: {string.Join(", ", args.Skip(3))}. {Usage}");

        return new ConsoleCommand("create-user", Username: args[1].Trim(), Password: args[2]);
    }

    private static ConsoleCommand ParseExportBackup(string[] args)
    {
        if (args.Length < 2)
            throw new InvalidOperationException($"Missing backup path. {Usage}");

        var csvPath = args[1].Trim();
        var remainingArgs = args.Skip(2).ToList();

        var username = ExtractNamedArgument(remainingArgs, "--username");
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException($"Missing required --username argument. {Usage}");

        if (remainingArgs.Count > 0)
            throw new InvalidOperationException($"Unexpected arguments: {string.Join(", ", remainingArgs)}. {Usage}");

        return new ConsoleCommand("export-backup", CsvPath: csvPath, Username: username);
    }

    private static ConsoleCommand ParseImportBackup(string[] args)
    {
        if (args.Length < 2)
            throw new InvalidOperationException($"Missing backup path. {Usage}");

        var csvPath = args[1].Trim();
        var remainingArgs = args.Skip(2).ToList();

        var username = ExtractNamedArgument(remainingArgs, "--username");
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException($"Missing required --username argument. {Usage}");

        var dryRun = remainingArgs.RemoveAll(argument => string.Equals(argument, "--dry-run", StringComparison.OrdinalIgnoreCase)) > 0;

        if (remainingArgs.Count > 0)
            throw new InvalidOperationException($"Unexpected arguments: {string.Join(", ", remainingArgs)}. {Usage}");

        return new ConsoleCommand("import-backup", CsvPath: csvPath, Username: username, DryRun: dryRun);
    }

    private static ConsoleCommand ParseResetDatabase(string[] args)
    {
        var remainingArgs = args.Skip(1).ToList();
        var skipConfirmation = remainingArgs.RemoveAll(argument => string.Equals(argument, "--yes", StringComparison.OrdinalIgnoreCase)) > 0;

        if (remainingArgs.Count > 0)
            throw new InvalidOperationException($"Unexpected arguments: {string.Join(", ", remainingArgs)}. {Usage}");

        return new ConsoleCommand("reset-database", SkipConfirmation: skipConfirmation);
    }

    private static string? ExtractNamedArgument(List<string> args, string name)
    {
        var index = args.FindIndex(argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || index == args.Count - 1)
            return null;

        var value = args[index + 1];
        args.RemoveRange(index, 2);
        return value;
    }
}
