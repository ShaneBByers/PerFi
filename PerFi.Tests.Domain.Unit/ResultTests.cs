using PerFi.Domain.Results;
using Xunit;

namespace PerFi.Tests.Domain.Unit;

public class ResultTests
{
    [Fact]
    public void Success_SetsIsSuccessAndClearsError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_SetsIsFailureAndError()
    {
        var result = Result.Failure("boom");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("boom", result.Error);
    }

    [Fact]
    public void GenericSuccess_SetsValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void GenericFailure_SetsErrorAndDefaultValue()
    {
        var result = Result<int>.Failure("boom");

        Assert.True(result.IsFailure);
        Assert.Equal("boom", result.Error);
        Assert.Equal(0, result.Value);
    }
}
