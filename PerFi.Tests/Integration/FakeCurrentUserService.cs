using PerFi.Domain.Interfaces;

namespace PerFi.Tests.Integration;

internal sealed class FakeCurrentUserService(string userId = FakeCurrentUserService.DefaultUserId) : ICurrentUserService
{
    public const string DefaultUserId = "test-user-id";

    public string UserId { get; } = userId;
}
