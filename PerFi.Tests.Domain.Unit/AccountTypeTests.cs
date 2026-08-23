using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain.Unit;

public class AccountTypeTests
{
    private static readonly AccountTypeGroup TestGroup = new("Assets");

    [Fact]
    public void CreatingAccountType_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AccountType("   ", TestGroup));
    }

    [Fact]
    public void CreatingAccountType_WithNullGroup_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AccountType("Checking", null!));
    }

    [Fact]
    public void CreatingAccountType_TrimsName()
    {
        var accountType = new AccountType("  Checking  ", TestGroup);

        Assert.Equal("Checking", accountType.Name);
    }

    [Fact]
    public void CreatingAccountType_WithIdOverload_SetsId()
    {
        var accountType = new AccountType(5, "Checking", TestGroup);

        Assert.Equal(5, accountType.Id);
        Assert.Same(TestGroup, accountType.Group);
    }
}
