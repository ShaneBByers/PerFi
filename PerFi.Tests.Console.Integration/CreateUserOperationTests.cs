using PerFi.Console.Operations;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.Console.Integration;

public sealed class CreateUserOperationTests : IDisposable
{
    private readonly Microsoft.AspNetCore.Identity.UserManager<PerFi.Infrastructure.Entities.ApplicationUser> _userManager;
    private readonly PerFi.Infrastructure.PerFiDbContext _dbContext;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    public CreateUserOperationTests()
    {
        (_userManager, _dbContext, _connection) = IdentityTestHelper.CreateUserManager();
    }

    [Fact]
    public async Task ExecuteAsync_WithBlankUsername_ThrowsArgumentException()
    {
        var operation = new CreateUserOperation(_userManager);

        await Assert.ThrowsAsync<ArgumentException>(() => operation.ExecuteAsync("   ", "Test-Password1!"));
    }

    [Fact]
    public async Task ExecuteAsync_WithBlankPassword_ThrowsArgumentException()
    {
        var operation = new CreateUserOperation(_userManager);

        await Assert.ThrowsAsync<ArgumentException>(() => operation.ExecuteAsync("shane", "   "));
    }

    [Fact]
    public async Task ExecuteAsync_WithNewUser_CreatesUser()
    {
        var operation = new CreateUserOperation(_userManager);

        await operation.ExecuteAsync("shane", "Test-Password1!");

        var user = await _userManager.FindByNameAsync("shane");
        Assert.NotNull(user);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingUsername_ThrowsInvalidOperationException()
    {
        var operation = new CreateUserOperation(_userManager);
        await operation.ExecuteAsync("shane", "Test-Password1!");

        await Assert.ThrowsAsync<InvalidOperationException>(() => operation.ExecuteAsync("shane", "Test-Password1!"));
    }

    [Fact]
    public async Task ExecuteAsync_WithWeakPassword_ThrowsInvalidOperationException()
    {
        var operation = new CreateUserOperation(_userManager);

        await Assert.ThrowsAsync<InvalidOperationException>(() => operation.ExecuteAsync("shane", "a"));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
