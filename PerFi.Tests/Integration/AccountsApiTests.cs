using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PerFi.API.Requests;
using Xunit;

namespace PerFi.Tests.Integration;

public class AccountsApiTests : IClassFixture<PerFiApiFactory>
{
    private readonly HttpClient _client;

    public AccountsApiTests(PerFiApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task PostAccount_WithMissingName_ReturnsBadRequest()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { username = PerFiApiFactory.TestUsername, password = PerFiApiFactory.TestPassword });
        var token = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest("   ", 1, 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccounts_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = PerFiApiFactory.TestUsername, password = PerFiApiFactory.TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.TryGetProperty("token", out var tokenProperty));
        Assert.False(string.IsNullOrWhiteSpace(tokenProperty.GetString()));
    }
}
