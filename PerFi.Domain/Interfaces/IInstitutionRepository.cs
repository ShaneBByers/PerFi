using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Domain.Interfaces;

public interface IInstitutionRepository
{
    Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default);
    Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> AddInstitutionAsync(Institution institution, CancellationToken cancellationToken = default);
    Task<Result> UpdateInstitutionAsync(Institution institution, CancellationToken cancellationToken = default);
    Task<Result> DeleteInstitutionAsync(int institutionId, CancellationToken cancellationToken = default);
}