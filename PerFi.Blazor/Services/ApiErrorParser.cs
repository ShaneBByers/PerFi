using System.Text.Json;

namespace PerFi.Blazor.Services;

public static class ApiErrorParser
{
    public static async Task<ApiResult> FromFailedResponseAsync(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(responseBody))
            return ApiResult.Failure($"Request failed with status code {(int)response.StatusCode}.");

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
            {
                var errors = new Dictionary<string, string[]>();
                foreach (var property in errorsElement.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    var values = property.Value.EnumerateArray()
                        .Where(v => v.ValueKind == JsonValueKind.String)
                        .Select(v => v.GetString() ?? string.Empty)
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .ToArray();

                    if (values.Length > 0)
                        errors[property.Name] = values;
                }

                var title = root.TryGetProperty("title", out var titleElement)
                    ? titleElement.GetString() ?? "Validation failed."
                    : "Validation failed.";

                return ApiResult.Failure(title, errors);
            }

            if (root.TryGetProperty("detail", out var detailElement) && detailElement.ValueKind == JsonValueKind.String)
                return ApiResult.Failure(detailElement.GetString() ?? "Request failed.");

            if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.String)
                return ApiResult.Failure(errorElement.GetString() ?? "Request failed.");
        }
        catch (JsonException)
        {
            // Response body wasn't JSON, use fallback below.
        }

        return ApiResult.Failure(responseBody);
    }
}
