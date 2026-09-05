using System.Net.Http.Json;
using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public sealed class SalaryProgressionsApiClient(HttpClient httpClient) : ISalaryProgressionsApiClient
{
    public async Task<IReadOnlyList<SalaryProgressionResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<SalaryProgressionResponse>>("api/salaryprogressions", cancellationToken) ?? [];

    public Task<SalaryProgressionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<SalaryProgressionResponse>($"api/salaryprogressions/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(DateOnly effectiveDate, decimal annualSalary, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/salaryprogressions", new CreateSalaryProgressionRequest(effectiveDate, annualSalary), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> UpdateAsync(int id, DateOnly effectiveDate, decimal annualSalary, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/salaryprogressions/{id}", new UpdateSalaryProgressionRequest(effectiveDate, annualSalary), cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/salaryprogressions/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success();

        return await ApiErrorParser.FromFailedResponseAsync(response);
    }
}
