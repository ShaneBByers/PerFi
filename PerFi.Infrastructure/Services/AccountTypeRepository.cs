using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class AccountTypeRepository(
    PerFiDbContext dbContext)
    : IAccountTypeRepository
{
    public async Task<IReadOnlyList<AccountType>> GetAllAccountTypesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AccountTypes
            .Select(at => new AccountType(at.Id, at.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountType?> GetAccountTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var accountTypeEntity = await dbContext.AccountTypes
            .FirstOrDefaultAsync(at => at.Id == id, cancellationToken);

        if (accountTypeEntity == null)
            return null;

        return new AccountType(accountTypeEntity.Id, accountTypeEntity.Name);
    }

    public async Task<Result<int>> AddAccountTypeAsync(AccountType accountType, CancellationToken cancellationToken = default)
    {
        if (await dbContext.AccountTypes.AnyAsync(at => at.Name == accountType.Name, cancellationToken))
            return Result<int>.Failure($"An account type with name '{accountType.Name}' already exists.");

        var newAccountType = new AccountTypeEntity
        {
            Name = accountType.Name
        };

        dbContext.AccountTypes.Add(newAccountType);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(newAccountType.Id);
    }
}