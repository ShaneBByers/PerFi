using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain.Unit;

public class AccountTests
{
    private static readonly AccountTypeGroup TestGroup = new("Assets");

    [Fact]
    public void CreatingAccount_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Account("   ", new AccountType("Checking", TestGroup)));
    }

    [Fact]
    public void CreatingAccount_WithNullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Account("Checking", null!));
    }

    [Fact]
    public void CreatingAccount_TrimsName()
    {
        var account = new Account("  Checking  ", new AccountType("Checking", TestGroup));

        Assert.Equal("Checking", account.Name);
    }

    [Fact]
    public void CreatingAccount_WithIdOverload_SetsIdAndInstitutionId()
    {
        var accountType = new AccountType("Checking", TestGroup);

        var account = new Account(42, "Checking", accountType, 7);

        Assert.Equal(42, account.Id);
        Assert.Equal(7, account.InstitutionId);
        Assert.Same(accountType, account.Type);
    }
}
