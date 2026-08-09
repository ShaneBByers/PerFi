using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface IAccountTypeService
{
    Task<IReadOnlyList<AccountType>> GetAllAccountTypesAsync(CancellationToken cancellationToken = default);
    Task<AccountType?> GetAccountTypeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AccountType>> CreateAccountTypeAsync(CreateAccountTypeCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateAccountTypeAsync(UpdateAccountTypeCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteAccountTypeAsync(int accountTypeId, CancellationToken cancellationToken = default);
    Task<Result> ReorderAccountTypesAsync(ReorderAccountTypeCommand command, CancellationToken cancellationToken = default);
}