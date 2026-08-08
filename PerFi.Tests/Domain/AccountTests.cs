using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain;

public class AccountTests
{
    [Fact]
    public void CreatingAccount_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Account("   ", new AccountType("Checking")));
    }

    [Fact]
    public void CreatingAccount_WithNullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Account("Checking", null!));
    }
}
