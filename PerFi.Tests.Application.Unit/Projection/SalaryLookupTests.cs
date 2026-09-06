using PerFi.Application.Services.Projection;
using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Application.Unit.Projection;

public class SalaryLookupTests
{
    [Fact]
    public void GetSalaryAt_WithNoRows_ReturnsZero()
    {
        var result = SalaryLookup.GetSalaryAt([], 3m, new DateOnly(2026, 1, 1));

        Assert.Equal(0m, result);
    }

    [Fact]
    public void GetSalaryAt_BeforeEarliestRow_ClampsFlat()
    {
        List<SalaryProgression> progressions = [new(new DateOnly(2024, 1, 1), 80000m)];

        var result = SalaryLookup.GetSalaryAt(progressions, 3m, new DateOnly(2020, 1, 1));

        Assert.Equal(80000m, result);
    }

    [Fact]
    public void GetSalaryAt_BetweenTwoKnownRows_StaysFlatWithoutEscalation()
    {
        List<SalaryProgression> progressions =
        [
            new(new DateOnly(2024, 1, 1), 80000m),
            new(new DateOnly(2027, 1, 1), 90000m)
        ];

        var result = SalaryLookup.GetSalaryAt(progressions, 10m, new DateOnly(2025, 6, 1));

        Assert.Equal(80000m, result);
    }

    [Fact]
    public void GetSalaryAt_BeyondLastKnownRow_CompoundsAnnually()
    {
        List<SalaryProgression> progressions = [new(new DateOnly(2024, 1, 1), 100000m)];

        var result = SalaryLookup.GetSalaryAt(progressions, 10m, new DateOnly(2026, 1, 1));

        Assert.Equal(121000m, Math.Round(result, 2));
    }

    [Fact]
    public void GetSalaryAt_SameYearAsLastKnownRow_DoesNotEscalateYet()
    {
        List<SalaryProgression> progressions = [new(new DateOnly(2026, 1, 1), 100000m)];

        var result = SalaryLookup.GetSalaryAt(progressions, 10m, new DateOnly(2026, 11, 1));

        Assert.Equal(100000m, result);
    }
}
