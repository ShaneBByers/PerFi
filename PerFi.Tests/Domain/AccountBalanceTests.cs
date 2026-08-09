using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain;

public class AccountBalanceTests
{
    [Fact]
    public void CreatingAccountBalance_WithNegativeValue_AllowsBalance()
    {
        var account = new Account("Credit Card", new AccountType("Credit"));

        var accountBalance = new AccountBalance(account, -125.50m);

        Assert.Equal(-125.50m, accountBalance.Balance);
    }

    [Fact]
    public void CreatingAccountBalance_WithNullAccount_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AccountBalance(null!, 0m));
    }
}
