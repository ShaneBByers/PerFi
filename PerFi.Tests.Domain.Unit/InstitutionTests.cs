using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain.Unit;

public class InstitutionTests
{
    [Fact]
    public void CreatingInstitution_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Institution("   ", []));
    }

    [Fact]
    public void CreatingInstitution_WithNullAccounts_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Institution("First Bank", null!));
    }

    [Fact]
    public void CreatingInstitution_TrimsName()
    {
        var institution = new Institution("  First Bank  ", []);

        Assert.Equal("First Bank", institution.Name);
    }

    [Fact]
    public void CreatingInstitution_WithIdOverload_SetsIdAndPassesThroughAccounts()
    {
        var accountType = new AccountType("Checking", new AccountTypeGroup("Assets"));
        var accounts = new List<Account> { new(1, "Checking", accountType, 9) };

        var institution = new Institution(9, "First Bank", accounts);

        Assert.Equal(9, institution.Id);
        Assert.Same(accounts, institution.Accounts);
    }
}
