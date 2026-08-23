using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain.Unit;

public class AccountTypeGroupTests
{
    [Fact]
    public void CreatingAccountTypeGroup_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AccountTypeGroup("   "));
    }

    [Fact]
    public void CreatingAccountTypeGroup_TrimsName()
    {
        var group = new AccountTypeGroup("  Assets  ");

        Assert.Equal("Assets", group.Name);
    }

    [Fact]
    public void CreatingAccountTypeGroup_WithIdOverload_SetsId()
    {
        var group = new AccountTypeGroup(3, "Assets");

        Assert.Equal(3, group.Id);
        Assert.Equal("Assets", group.Name);
    }
}
