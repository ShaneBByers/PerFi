using PerFi.Domain.Interfaces;

namespace PerFi.Console;

// Console operations have no HttpContext, so the owning user id is set explicitly before running an operation.
public sealed class ConsoleCurrentUserService : ICurrentUserService
{
    private string? _userId;

    public string UserId
    {
        get => _userId ?? throw new InvalidOperationException("The current user has not been set for this console operation.");
        set => _userId = value;
    }
}
