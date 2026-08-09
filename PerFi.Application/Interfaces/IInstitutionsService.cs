using PerFi.Application.Commands;
using PerFi.Domain.Entities;
using PerFi.Domain.Results;

namespace PerFi.Application.Interfaces;

public interface IInstitutionService
{
    Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default);
    Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Institution>> CreateInstitutionAsync(CreateInstitutionCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateInstitutionAsync(UpdateInstitutionCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteInstitutionAsync(int institutionId, CancellationToken cancellationToken = default);
}