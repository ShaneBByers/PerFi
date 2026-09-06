namespace PerFi.Blazor.Services;

public enum ToastLevel
{
    Success,
    Error,
    Warning,
    Info
}

public sealed record ToastMessage(Guid Id, ToastLevel Level, string Message);
