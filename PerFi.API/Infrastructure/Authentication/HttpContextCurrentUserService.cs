using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PerFi.Domain.Interfaces;

namespace PerFi.API.Infrastructure.Authentication;

public sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("No authenticated user is available on the current request.");
}
