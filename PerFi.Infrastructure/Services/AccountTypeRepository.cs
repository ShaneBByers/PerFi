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

    public async Task<Result> UpdateAccountTypeAsync(AccountType accountType, CancellationToken cancellationToken = default)
    {
        var accountTypeEntity = await dbContext.AccountTypes
            .FirstOrDefaultAsync(at => at.Id == accountType.Id, cancellationToken);

        if (accountTypeEntity is null)
            return Result.Failure($"Account type with ID '{accountType.Id}' not found.");

        var hasDuplicateName = await dbContext.AccountTypes
            .AnyAsync(at => at.Id != accountType.Id && at.Name == accountType.Name, cancellationToken);

        if (hasDuplicateName)
            return Result.Failure($"An account type with name '{accountType.Name}' already exists.");

        accountTypeEntity.Name = accountType.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAccountTypeAsync(int accountTypeId, CancellationToken cancellationToken = default)
    {
        var accountTypeEntity = await dbContext.AccountTypes
            .FirstOrDefaultAsync(at => at.Id == accountTypeId, cancellationToken);

        if (accountTypeEntity is null)
            return Result.Failure($"Account type with ID '{accountTypeId}' not found.");

        var isReferenced = await dbContext.Accounts
            .AnyAsync(a => EF.Property<int>(a, "TypeId") == accountTypeId, cancellationToken);

        if (isReferenced)
            return Result.Failure("Cannot delete account type because one or more accounts reference it.");

        dbContext.AccountTypes.Remove(accountTypeEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}