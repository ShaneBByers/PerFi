using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IAccountTypeRepository
{
    Task<IReadOnlyList<AccountType>> GetAllAccountTypesAsync(CancellationToken cancellationToken = default);
    Task<AccountType?> GetAccountTypeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddAccountTypeAsync(AccountType accountType, CancellationToken cancellationToken = default);
}