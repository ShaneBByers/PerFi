using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using PerFi.Tests.Shared;
using Xunit;

namespace PerFi.Tests.API.Integration;

public sealed class HealthEndpointsTests(PerFiApiFactory factory) : IClassFixture<PerFiApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task GetLiveness_ReturnsOkWithoutAuthentication()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
