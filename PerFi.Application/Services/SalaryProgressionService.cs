using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class SalaryProgressionService(ISalaryProgressionRepository salaryProgressionRepository)
    : ISalaryProgressionService
{
    public async Task<IReadOnlyList<SalaryProgression>> GetAllSalaryProgressionsAsync(CancellationToken cancellationToken = default)
        => await salaryProgressionRepository.GetAllSalaryProgressionsAsync(cancellationToken);

    public async Task<SalaryProgression?> GetSalaryProgressionByIdAsync(int id, CancellationToken cancellationToken = default)
        => await salaryProgressionRepository.GetSalaryProgressionByIdAsync(id, cancellationToken);

    public async Task<Result<SalaryProgression>> CreateSalaryProgressionAsync(CreateSalaryProgressionCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<SalaryProgression>.Failure("Create salary progression command cannot be null.");

        try
        {
            var salaryProgression = new SalaryProgression(command.EffectiveDate, command.AnnualSalary);
            var result = await salaryProgressionRepository.AddSalaryProgressionAsync(salaryProgression, cancellationToken);

            if (!result.IsSuccess)
                return Result<SalaryProgression>.Failure(result.Error);

            salaryProgression.Id = result.Value;
            return Result<SalaryProgression>.Success(salaryProgression);
        }
        catch (ArgumentException ex)
        {
            return Result<SalaryProgression>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateSalaryProgressionAsync(UpdateSalaryProgressionCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Update salary progression command cannot be null.");

        try
        {
            var salaryProgression = new SalaryProgression(command.SalaryProgressionId, command.EffectiveDate, command.AnnualSalary);
            return await salaryProgressionRepository.UpdateSalaryProgressionAsync(salaryProgression, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteSalaryProgressionAsync(int salaryProgressionId, CancellationToken cancellationToken = default)
        => await salaryProgressionRepository.DeleteSalaryProgressionAsync(salaryProgressionId, cancellationToken);
}
