using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class InstitutionsApiClient(HttpClient httpClient) : IInstitutionsApiClient
{
    public async Task<IReadOnlyList<InstitutionResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<InstitutionResponse>>("api/institutions", cancellationToken) ?? [];

    public Task<InstitutionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<InstitutionResponse>($"api/institutions/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/institutions", new CreateInstitutionRequest(name), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/institutions/{id}", new UpdateInstitutionRequest(name), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/institutions/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedInstitutionIds, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/institutions/reorder", new ReorderInstitutionsRequest(orderedInstitutionIds), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
