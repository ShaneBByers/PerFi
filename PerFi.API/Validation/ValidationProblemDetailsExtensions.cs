using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PerFi.API.Validation;

public static class ValidationProblemDetailsExtensions
{
    public static ValidationProblemDetails ToValidationProblemDetails(this IReadOnlyDictionary<string, string[]> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var (key, value) in errors)
        {
            modelState.AddModelError(key, string.Join(" ", value));
        }

        return new ValidationProblemDetails(modelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        };
    }
}
