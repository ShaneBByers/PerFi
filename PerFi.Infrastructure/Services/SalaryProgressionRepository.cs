using Microsoft.EntityFrameworkCore;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Services;

internal class SalaryProgressionRepository(
    PerFiDbContext dbContext,
    ICurrentUserService currentUserService)
    : ISalaryProgressionRepository
{
    public async Task<IReadOnlyList<SalaryProgression>> GetAllSalaryProgressionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SalaryProgressions
            .AsNoTracking()
            .Where(salaryProgression => salaryProgression.UserId == currentUserService.UserId)
            .OrderBy(salaryProgression => salaryProgression.EffectiveDate)
            .ThenBy(salaryProgression => salaryProgression.Id)
            .Select(salaryProgression => new SalaryProgression(
                salaryProgression.Id,
                salaryProgression.EffectiveDate,
                salaryProgression.AnnualSalary))
            .ToListAsync(cancellationToken);
    }

    public async Task<SalaryProgression?> GetSalaryProgressionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SalaryProgressions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                salaryProgression => salaryProgression.Id == id && salaryProgression.UserId == currentUserService.UserId,
                cancellationToken);

        return entity is null
            ? null
            : new SalaryProgression(entity.Id, entity.EffectiveDate, entity.AnnualSalary);
    }

    public async Task<Result<int>> AddSalaryProgressionAsync(SalaryProgression salaryProgression, CancellationToken cancellationToken = default)
    {
        if (await dbContext.SalaryProgressions.AnyAsync(
                existing => existing.UserId == currentUserService.UserId && existing.EffectiveDate == salaryProgression.EffectiveDate,
                cancellationToken))
        {
            return Result<int>.Failure($"A salary progression entry for '{salaryProgression.EffectiveDate}' already exists.");
        }

        var entity = new SalaryProgressionEntity
        {
            EffectiveDate = salaryProgression.EffectiveDate,
            AnnualSalary = salaryProgression.AnnualSalary,
            UserId = currentUserService.UserId
        };

        dbContext.SalaryProgressions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateSalaryProgressionAsync(SalaryProgression salaryProgression, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SalaryProgressions
            .FirstOrDefaultAsync(
                existing => existing.Id == salaryProgression.Id && existing.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"Salary progression with ID '{salaryProgression.Id}' not found.");

        if (await dbContext.SalaryProgressions.AnyAsync(
                existing => existing.UserId == currentUserService.UserId
                            && existing.EffectiveDate == salaryProgression.EffectiveDate
                            && existing.Id != salaryProgression.Id,
                cancellationToken))
        {
            return Result.Failure($"A salary progression entry for '{salaryProgression.EffectiveDate}' already exists.");
        }

        entity.EffectiveDate = salaryProgression.EffectiveDate;
        entity.AnnualSalary = salaryProgression.AnnualSalary;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteSalaryProgressionAsync(int salaryProgressionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SalaryProgressions
            .FirstOrDefaultAsync(
                salaryProgression => salaryProgression.Id == salaryProgressionId && salaryProgression.UserId == currentUserService.UserId,
                cancellationToken);

        if (entity is null)
            return Result.Failure($"Salary progression with ID '{salaryProgressionId}' not found.");

        dbContext.SalaryProgressions.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
