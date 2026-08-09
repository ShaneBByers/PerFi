using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class InstitutionService(
    IInstitutionRepository institutionRepository)
    : IInstitutionService
{
    public async Task<IReadOnlyList<Institution>> GetAllInstitutionsAsync(CancellationToken cancellationToken = default)
        => await institutionRepository.GetAllInstitutionsAsync(cancellationToken);

    public async Task<Institution?> GetInstitutionByIdAsync(int id, CancellationToken cancellationToken = default)
        => await institutionRepository.GetInstitutionByIdAsync(id, cancellationToken);

    public async Task<Result<Institution>> CreateInstitutionAsync(CreateInstitutionCommand command, CancellationToken cancellationToken = default)
    {
        Institution institution;

        try
        {
            institution = new Institution(command.InstitutionName, []);
        }
        catch (ArgumentException ex) { return Result<Institution>.Failure(ex.Message); }

        Result<int> result = await institutionRepository.AddInstitutionAsync(
            institution,
            cancellationToken);

        if (!result.IsSuccess)
            return Result<Institution>.Failure(result.Error);

        institution.Id = result.Value;

        return Result<Institution>.Success(institution);
    }

    public async Task<Result> UpdateInstitutionAsync(UpdateInstitutionCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await institutionRepository.GetInstitutionByIdAsync(command.InstitutionId, cancellationToken);
            if (existing is null)
                return Result.Failure($"Institution with ID '{command.InstitutionId}' not found.");

            var institution = new Institution(command.InstitutionId, command.InstitutionName, existing.Accounts);
            return await institutionRepository.UpdateInstitutionAsync(institution, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteInstitutionAsync(int institutionId, CancellationToken cancellationToken = default)
        => await institutionRepository.DeleteInstitutionAsync(institutionId, cancellationToken);

    public async Task<Result> ReorderInstitutionsAsync(ReorderInstitutionCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Reorder institutions command cannot be null.");

        return await institutionRepository.ReorderInstitutionsAsync(command.OrderedInstitutionIds, cancellationToken);
    }
}