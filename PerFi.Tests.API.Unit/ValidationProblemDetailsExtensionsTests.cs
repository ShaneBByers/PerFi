using PerFi.API.Validation;
using Xunit;

namespace PerFi.Tests.API.Unit;

public class ValidationProblemDetailsExtensionsTests
{
    [Fact]
    public void ToValidationProblemDetails_MapsErrorsAndStatus()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["accountName"] = ["Account name is required."]
        };

        var problemDetails = errors.ToValidationProblemDetails();

        Assert.Equal(400, problemDetails.Status);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.True(problemDetails.Errors.ContainsKey("accountName"));
        Assert.Contains("Account name is required.", problemDetails.Errors["accountName"]);
    }
}
