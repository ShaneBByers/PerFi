namespace PerFi.Blazor.Services;

public sealed record ApiResult(bool IsSuccess, string? ErrorMessage, IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static ApiResult Success() => new(true, null, null);

    public static ApiResult Failure(string errorMessage, IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        new(false, errorMessage, validationErrors);
}
