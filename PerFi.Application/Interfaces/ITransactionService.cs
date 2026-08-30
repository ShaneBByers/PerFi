using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface ITransactionService
{
    Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default);
    Task<Transaction?> GetTransactionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Transaction>> CreateTransactionAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateTransactionAsync(UpdateTransactionCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteTransactionAsync(int transactionId, CancellationToken cancellationToken = default);
}