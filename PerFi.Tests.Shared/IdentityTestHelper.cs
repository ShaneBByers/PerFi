using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PerFi.Infrastructure;
using PerFi.Infrastructure.Entities;

namespace PerFi.Tests.Shared;

/// <summary>
/// Builds a real <see cref="UserManager{TUser}"/> backed by a Sqlite in-memory <see cref="PerFiDbContext"/>,
/// for tests that need genuine Identity behavior (password hashing, lockout, validation) without a full web host.
/// </summary>
public static class IdentityTestHelper
{
    public static (UserManager<ApplicationUser> UserManager, PerFiDbContext DbContext, SqliteConnection Connection) CreateUserManager()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<PerFiDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new PerFiDbContext(options);
        dbContext.Database.EnsureCreated();

        var userStore = new UserStore<ApplicationUser>(dbContext);
        var identityOptions = new OptionsWrapper<IdentityOptions>(new IdentityOptions());
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
        var normalizer = new UpperInvariantLookupNormalizer();

        var userManager = new UserManager<ApplicationUser>(
            userStore,
            identityOptions,
            passwordHasher,
            userValidators,
            passwordValidators,
            normalizer,
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        return (userManager, dbContext, connection);
    }
}
