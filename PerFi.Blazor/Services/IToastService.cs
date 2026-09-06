namespace PerFi.Blazor.Services;

public interface IToastService
{
    IReadOnlyList<ToastMessage> Toasts { get; }

    event Action? OnChange;

    void ShowSuccess(string message);

    void ShowError(string message);

    void ShowWarning(string message);

    // Drop-in replacement for the old `errorMessage = ...; validationErrors = ...;` pattern.
    void ShowFailure(string? errorMessage, IReadOnlyDictionary<string, string[]>? validationErrors = null);

    void Dismiss(Guid id);
}
