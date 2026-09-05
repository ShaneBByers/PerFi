using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface ISalaryProgressionService
{
    Task<IReadOnlyList<SalaryProgression>> GetAllSalaryProgressionsAsync(CancellationToken cancellationToken = default);
    Task<SalaryProgression?> GetSalaryProgressionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<SalaryProgression>> CreateSalaryProgressionAsync(CreateSalaryProgressionCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateSalaryProgressionAsync(UpdateSalaryProgressionCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteSalaryProgressionAsync(int salaryProgressionId, CancellationToken cancellationToken = default);
}
