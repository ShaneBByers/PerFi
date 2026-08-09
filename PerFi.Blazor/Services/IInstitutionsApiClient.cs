using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface IInstitutionsApiClient
{
    Task<IReadOnlyList<InstitutionResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InstitutionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, string name, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> ReorderAsync(IReadOnlyList<int> orderedInstitutionIds, CancellationToken cancellationToken = default);
}
