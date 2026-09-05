using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface ISalaryProgressionsApiClient
{
    Task<IReadOnlyList<SalaryProgressionResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SalaryProgressionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(DateOnly effectiveDate, decimal annualSalary, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, DateOnly effectiveDate, decimal annualSalary, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
