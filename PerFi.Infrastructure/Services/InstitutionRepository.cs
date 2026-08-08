using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class InstitutionRepository(
    PerFiDbContext dbContext)
    : IInstitutionRepository
{
    public async Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Institutions
            .Include(i => i.Accounts)
                .ThenInclude(a => a.Type)
            .Select(i => new Institution(i.Id, i.Name, i.Accounts.Select(a => new Account(a.Id, a.Name, new AccountType(a.Type.Id, a.Type.Name))).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var institutionEntity = await dbContext.Institutions
            .Include(i => i.Accounts)
                .ThenInclude(a => a.Type)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (institutionEntity == null)
            return null;

        return new Institution(institutionEntity.Id, institutionEntity.Name, [.. institutionEntity.Accounts.Select(a => new Account(a.Id, a.Name, new AccountType(a.Type.Id, a.Type.Name)))]);
    }

    public async Task<Result<int>> AddInstitutionAsync(Institution institution, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Institutions.AnyAsync(i => i.Name == institution.Name, cancellationToken))
            return Result<int>.Failure($"An institution with name '{institution.Name}' already exists.");

        var newInstitution = new InstitutionEntity
        {
            Name = institution.Name,
            Accounts = [.. institution.Accounts.Select(a => new AccountEntity
            {
                Name = a.Name,
                Type = new AccountTypeEntity
                {
                    Name = a.Type.Name
                }
            })]
        };

        dbContext.Institutions.Add(newInstitution);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(newInstitution.Id);
    }
}