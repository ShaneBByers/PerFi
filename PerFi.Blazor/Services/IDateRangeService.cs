namespace PerFi.Blazor.Services;

// Single global date range shared across every page (Cash Flow, Net Worth, etc.).
public interface IDateRangeService
{
    string Preset { get; }

    DateTime CustomStart { get; }

    DateTime CustomEnd { get; }

    bool IsOpen { get; }

    event Action? OnChange;

    void SetPreset(string preset);

    void SetCustomRange(DateTime start, DateTime end);

    void Toggle();

    void Close();

    // Returns null for "all" (no filtering should be applied).
    (DateOnly Start, DateOnly End)? Resolve(DateOnly latestDate);

    string GetButtonLabel();

    string GetDescriptionLabel(DateOnly latestDate);
}
