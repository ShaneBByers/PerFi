using System.Text;

namespace PerFi.Blazor.Services;

public sealed class ToastService : IToastService
{
    private static readonly TimeSpan ShortDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan LongDuration = TimeSpan.FromSeconds(7);

    private readonly List<ToastMessage> toasts = [];

    public IReadOnlyList<ToastMessage> Toasts => toasts;

    public event Action? OnChange;

    public void ShowSuccess(string message) => Show(ToastLevel.Success, message, ShortDuration);

    public void ShowError(string message) => Show(ToastLevel.Error, message, LongDuration);

    public void ShowWarning(string message) => Show(ToastLevel.Warning, message, LongDuration);

    public void ShowFailure(string? errorMessage, IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        var text = BuildFailureMessage(errorMessage, validationErrors);
        if (!string.IsNullOrWhiteSpace(text))
            ShowError(text);
    }

    public void Dismiss(Guid id)
    {
        if (toasts.RemoveAll(t => t.Id == id) > 0)
            OnChange?.Invoke();
    }

    private void Show(ToastLevel level, string message, TimeSpan duration)
    {
        var toast = new ToastMessage(Guid.NewGuid(), level, message);
        toasts.Add(toast);
        OnChange?.Invoke();

        _ = DismissAfterAsync(toast.Id, duration);
    }

    private async Task DismissAfterAsync(Guid id, TimeSpan duration)
    {
        await Task.Delay(duration);
        Dismiss(id);
    }

    private static string BuildFailureMessage(string? errorMessage, IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(errorMessage))
            sb.Append(errorMessage);

        if (validationErrors is { Count: > 0 })
        {
            foreach (var entry in validationErrors)
            {
                foreach (var message in entry.Value)
                {
                    if (sb.Length > 0)
                        sb.Append('\n');
                    sb.Append(entry.Key).Append(": ").Append(message);
                }
            }
        }

        return sb.ToString();
    }
}
