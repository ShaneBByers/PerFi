using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface IAccountsApiClient
{
    Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AccountResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(string accountName, int institutionId, int accountTypeId, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, string accountName, int institutionId, int accountTypeId, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
