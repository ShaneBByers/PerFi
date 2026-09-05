using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface ISalaryProgressionRepository
{
    Task<IReadOnlyList<SalaryProgression>> GetAllSalaryProgressionsAsync(CancellationToken cancellationToken = default);
    Task<SalaryProgression?> GetSalaryProgressionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddSalaryProgressionAsync(SalaryProgression salaryProgression, CancellationToken cancellationToken = default);
    Task<Result> UpdateSalaryProgressionAsync(SalaryProgression salaryProgression, CancellationToken cancellationToken = default);
    Task<Result> DeleteSalaryProgressionAsync(int salaryProgressionId, CancellationToken cancellationToken = default);
}
