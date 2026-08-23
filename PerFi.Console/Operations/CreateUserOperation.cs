using Microsoft.AspNetCore.Identity;
using PerFi.Infrastructure.Entities;

namespace PerFi.Console.Operations;

public sealed class CreateUserOperation(UserManager<ApplicationUser> userManager)
{
    public async Task ExecuteAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var existingUser = await userManager.FindByNameAsync(username);
        if (existingUser is not null)
            throw new InvalidOperationException($"User '{username}' already exists.");

        var user = new ApplicationUser { UserName = username };
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to create user '{username}': {errors}");
        }

        System.Console.WriteLine($"Created user '{username}' (Id: {user.Id}).");
    }
}
