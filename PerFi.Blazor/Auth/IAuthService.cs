namespace PerFi.Blazor.Auth;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<ApiLoginResult> LoginAsync(string username, string password);
    Task LogoutAsync();
}

public sealed record ApiLoginResult(bool IsSuccess, string? ErrorMessage)
{
    public static ApiLoginResult Success() => new(true, null);

    public static ApiLoginResult Failure(string message) => new(false, message);
}
