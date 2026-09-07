using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PerFi.API.Controllers;
using PerFi.API.Responses;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities.Projections;
using Xunit;

namespace PerFi.Tests.API.Unit;

public sealed class NetWorthProjectionControllerTests
{
    private static NetWorthProjectionController CreateController(RecordingNetWorthProjectionService service)
        => new(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task GetAll_ReturnsOk_WithMappedPeriods()
    {
        var totalAmounts = new ProjectionAmounts(1000m, 200m, 50m, 1250m);
        var totalGroup = new ProjectionGroup("Total", totalAmounts, totalAmounts, null, null);
        var groupAmounts = new ProjectionAmounts(600m, 150m, 30m, 780m);
        var expectedAmounts = new ProjectionAmounts(600m, 150m, 40m, 790m);
        var accountGroup = new ProjectionGroup("401k", groupAmounts, groupAmounts, expectedAmounts, expectedAmounts);
        var period = new ProjectionPeriod(
            ProjectionPeriodType.Annual,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            2026,
            null,
            false,
            35,
            36,
            35.5m,
            90_000m,
            93_000m,
            [accountGroup],
            totalGroup);

        var controller = CreateController(new RecordingNetWorthProjectionService { Periods = [period] });

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var responses = Assert.IsAssignableFrom<IEnumerable<ProjectionPeriodResponse>>(ok.Value).ToList();
        var response = Assert.Single(responses);

        Assert.Equal(ProjectionPeriodType.Annual, response.PeriodType);
        Assert.Equal(2026, response.CalendarYear);
        Assert.Null(response.Total.ExpectedAmounts);
        Assert.Null(response.Total.ExpectedAmountsInTodaysDollars);

        var mappedGroup = Assert.Single(response.Groups);
        Assert.Equal("401k", mappedGroup.GroupName);
        Assert.NotNull(mappedGroup.ExpectedAmounts);
        Assert.Equal(790m, mappedGroup.ExpectedAmounts!.FinalAmount);
    }

    private sealed class RecordingNetWorthProjectionService : INetWorthProjectionService
    {
        public IReadOnlyList<ProjectionPeriod> Periods { get; set; } = [];

        public Task<IReadOnlyList<ProjectionPeriod>> GetProjectionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Periods);
    }
}
