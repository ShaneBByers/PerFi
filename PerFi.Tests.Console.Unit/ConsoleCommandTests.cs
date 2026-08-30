using PerFi.Console;
using Xunit;

namespace PerFi.Tests.Console.Unit;

public class ConsoleCommandTests
{
    [Fact]
    public void Parse_WithNoArgs_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ConsoleCommand.Parse([]));
    }

    [Fact]
    public void Parse_WithUnknownVerb_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ConsoleCommand.Parse(["bogus"]));
    }

    [Fact]
    public void Parse_CreateUser_WithMissingPassword_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ConsoleCommand.Parse(["create-user", "shane"]));
    }

    [Fact]
    public void Parse_CreateUser_WithExtraArguments_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ConsoleCommand.Parse(["create-user", "shane", "pw", "extra"]));
    }

    [Fact]
    public void Parse_CreateUser_WithValidArgs_ParsesCommand()
    {
        var command = ConsoleCommand.Parse(["create-user", "shane", "Test-Password1!"]);

        Assert.Equal("create-user", command.Verb);
        Assert.Equal("shane", command.Username);
        Assert.Equal("Test-Password1!", command.Password);
    }

    [Fact]
    public void Parse_ResetDatabase_WithYesFlag_SetsSkipConfirmation()
    {
        var command = ConsoleCommand.Parse(["reset-database", "--yes"]);

        Assert.Equal("reset-database", command.Verb);
        Assert.True(command.SkipConfirmation);
    }

    [Fact]
    public void Parse_ResetDatabase_WithUnexpectedArguments_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ConsoleCommand.Parse(["reset-database", "--bogus"]));
    }
}
