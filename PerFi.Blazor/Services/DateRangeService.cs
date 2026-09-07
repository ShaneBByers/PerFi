namespace PerFi.Blazor.Services;

public sealed class DateRangeService : IDateRangeService
{
    public string Preset { get; private set; } = "ytd";

    public DateTime CustomStart { get; private set; } = new(DateTime.Today.Year, 1, 1);

    public DateTime CustomEnd { get; private set; } = DateTime.Today;

    public bool IsOpen { get; private set; }

    public event Action? OnChange;

    public void SetPreset(string preset)
    {
        Preset = preset;
        IsOpen = false;
        OnChange?.Invoke();
    }

    public void SetCustomRange(DateTime start, DateTime end)
    {
        if (end < start)
            (start, end) = (end, start);

        CustomStart = start;
        CustomEnd = end;
        Preset = "custom";
        IsOpen = false;
        OnChange?.Invoke();
    }

    // Open/close only affects this component's own render, not subscribers, so it must not raise OnChange.
    public void Toggle()
    {
        IsOpen = !IsOpen;
    }

    public void Close()
    {
        IsOpen = false;
    }

    public (DateOnly Start, DateOnly End)? Resolve(DateOnly latestDate)
    {
        if (Preset == "all")
            return null;

        if (Preset == "custom")
        {
            var start = DateOnly.FromDateTime(CustomStart);
            var end = DateOnly.FromDateTime(CustomEnd);
            return end < start ? (end, start) : (start, end);
        }

        if (Preset == "ytd")
            return (new DateOnly(latestDate.Year, 1, 1), latestDate);

        var months = Preset switch
        {
            "3m" => 3,
            "6m" => 6,
            _ => 12
        };

        return (latestDate.AddMonths(-months), latestDate);
    }

    public string GetButtonLabel()
    {
        return Preset switch
        {
            "ytd" => "Year to Date",
            "3m" => "Last 3 Months",
            "6m" => "Last 6 Months",
            "12m" => "Last 12 Months",
            "all" => "All Time",
            "custom" => $"{CustomStart:MMM d} \u2013 {CustomEnd:MMM d}",
            _ => "Select Range"
        };
    }

    public string GetDescriptionLabel(DateOnly latestDate)
    {
        if (Preset == "custom")
            return $"Custom: {CustomStart:MMM d, yyyy} to {CustomEnd:MMM d, yyyy}";

        var range = Resolve(latestDate);
        if (range is null)
            return "All Time";

        var label = Preset switch
        {
            "ytd" => "YTD",
            "3m" => "Last 3 Months",
            "6m" => "Last 6 Months",
            "12m" => "Last 12 Months",
            _ => "Selected Range"
        };

        return $"{label} ({range.Value.Start:MMM d} \u2013 {range.Value.End:MMM d, yyyy})";
    }
}
