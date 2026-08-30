using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default);
    Task<Transaction?> GetTransactionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<Result> UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<Result> DeleteTransactionAsync(int transactionId, CancellationToken cancellationToken = default);
}
