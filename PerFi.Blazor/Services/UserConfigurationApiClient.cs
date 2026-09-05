using System.Net;
using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class UserConfigurationApiClient(HttpClient httpClient) : IUserConfigurationApiClient
{
    public async Task<UserConfigurationResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/userconfiguration", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserConfigurationResponse>(cancellationToken);
    }

    public async Task<ApiResult> CreateAsync(CreateUserConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/userconfiguration", request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, UpdateUserConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/userconfiguration/{id}", request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
