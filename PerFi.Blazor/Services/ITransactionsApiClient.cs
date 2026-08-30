using PerFi.Blazor.Contracts;

namespace PerFi.Blazor.Services;

public interface ITransactionsApiClient
{
    Task<IReadOnlyList<TransactionResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TransactionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateAsync(DateOnly date, string counterpartyName, decimal amount, int transactionCategoryId, int accountId, string? description, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAsync(int id, DateOnly date, string counterpartyName, decimal amount, int transactionCategoryId, int accountId, string? description, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
