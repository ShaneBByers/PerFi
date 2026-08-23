using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain.Unit;

public class FinanceSnapshotTests
{
    private static AccountBalance CreateBalance(int accountId, decimal balance = 10m)
    {
        var accountType = new AccountType("Checking", new AccountTypeGroup("Assets"));
        var account = new Account(accountId, "Checking", accountType, 1);
        return new AccountBalance(account, balance);
    }

    [Fact]
    public void CreatingFinanceSnapshot_WithNullAccountBalances_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FinanceSnapshot(new DateOnly(2026, 1, 1), null!));
    }

    [Fact]
    public void CreatingFinanceSnapshot_WithEmptyAccountBalances_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FinanceSnapshot(new DateOnly(2026, 1, 1), []));
    }

    [Fact]
    public void CreatingFinanceSnapshot_WithDuplicateAccountIds_ThrowsArgumentException()
    {
        var balances = new List<AccountBalance> { CreateBalance(1), CreateBalance(1, 20m) };

        Assert.Throws<ArgumentException>(() => new FinanceSnapshot(new DateOnly(2026, 1, 1), balances));
    }

    [Fact]
    public void CreatingFinanceSnapshot_WithDefaultDate_ThrowsArgumentOutOfRangeException()
    {
        var balances = new List<AccountBalance> { CreateBalance(1) };

        Assert.Throws<ArgumentOutOfRangeException>(() => new FinanceSnapshot(default, balances));
    }

    [Fact]
    public void CreatingFinanceSnapshot_WithValidData_AssignsDateAndBalances()
    {
        var balances = new List<AccountBalance> { CreateBalance(1) };
        var date = new DateOnly(2026, 8, 9);

        var snapshot = new FinanceSnapshot(date, balances);

        Assert.Equal(date, snapshot.Date);
        Assert.Same(balances, snapshot.AccountBalances);
    }

    [Fact]
    public void CreatingFinanceSnapshot_WithIdOverload_SetsId()
    {
        var balances = new List<AccountBalance> { CreateBalance(1) };

        var snapshot = new FinanceSnapshot(7, new DateOnly(2026, 8, 9), balances);

        Assert.Equal(7, snapshot.Id);
    }
}
