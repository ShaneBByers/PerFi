using Microsoft.EntityFrameworkCore;
using PerFi.Infrastructure;

namespace PerFi.Console.Operations;

// Wipes all financial data (not user accounts) so migrations/tests can start from a clean slate.
public sealed class ResetDatabaseOperation(PerFiDbContext dbContext)
{
    public async Task ExecuteAsync(bool skipConfirmation, CancellationToken cancellationToken = default)
    {
        var institutionCount = await dbContext.Institutions.CountAsync(cancellationToken);
        var accountCount = await dbContext.Accounts.CountAsync(cancellationToken);
        var accountTypeCount = await dbContext.AccountTypes.CountAsync(cancellationToken);
        var accountTypeGroupCount = await dbContext.AccountTypeGroups.CountAsync(cancellationToken);
        var snapshotCount = await dbContext.FinanceSnapshots.CountAsync(cancellationToken);
        var balanceCount = await dbContext.AccountBalances.CountAsync(cancellationToken);

        System.Console.WriteLine("This will permanently delete ALL rows from:");
        System.Console.WriteLine($"- AccountBalances: {balanceCount}");
        System.Console.WriteLine($"- FinanceSnapshots: {snapshotCount}");
        System.Console.WriteLine($"- Accounts: {accountCount}");
        System.Console.WriteLine($"- Institutions: {institutionCount}");
        System.Console.WriteLine($"- AccountTypes: {accountTypeCount}");
        System.Console.WriteLine($"- AccountTypeGroups: {accountTypeGroupCount}");
        System.Console.WriteLine("User accounts (AspNetUsers) are not affected.");
        System.Console.WriteLine();

        if (!skipConfirmation)
        {
            System.Console.Write("Type RESET to confirm: ");
            var response = System.Console.ReadLine();

            if (!string.Equals(response?.Trim(), "RESET", StringComparison.Ordinal))
            {
                System.Console.WriteLine("Aborted. No changes were made.");
                return;
            }
        }

        await dbContext.AccountBalances.ExecuteDeleteAsync(cancellationToken);
        await dbContext.FinanceSnapshots.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Accounts.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Institutions.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AccountTypes.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AccountTypeGroups.ExecuteDeleteAsync(cancellationToken);

        System.Console.WriteLine("All financial data deleted.");
    }
}
