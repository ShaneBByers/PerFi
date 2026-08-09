using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface IAccountTypesApiClient
{
    Task<IReadOnlyList<AccountTypeResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AccountTypeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(string name, CancellationToken cancellationToken = default);
}
