using PerFi.Application.Commands;
using PerFi.Application.Interfaces;
using PerFi.Domain.Entities;
using PerFi.Domain.Interfaces;
using PerFi.Domain.Results;

namespace PerFi.Application.Services;

internal class ContributionService(
    IContributionRepository contributionRepository,
    IContributionContributorRepository contributionContributorRepository,
    IAccountRepository accountRepository)
    : IContributionService
{
    public async Task<IReadOnlyList<Contribution>> GetAllContributionsAsync(CancellationToken cancellationToken = default)
        => await contributionRepository.GetAllContributionsAsync(cancellationToken);

    public async Task<Contribution?> GetContributionByIdAsync(int id, CancellationToken cancellationToken = default)
        => await contributionRepository.GetContributionByIdAsync(id, cancellationToken);

    public async Task<Result<Contribution>> CreateContributionAsync(CreateContributionCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<Contribution>.Failure("Create contribution command cannot be null.");

        var contributor = await contributionContributorRepository.GetContributionContributorByIdAsync(command.ContributionContributorId, cancellationToken);
        if (contributor is null)
            return Result<Contribution>.Failure($"Contribution contributor with ID '{command.ContributionContributorId}' not found.");

        var account = await accountRepository.GetAccountByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
            return Result<Contribution>.Failure($"Account with ID '{command.AccountId}' not found.");

        try
        {
            var contribution = new Contribution(command.Date, command.Amount, contributor, command.AccountId);
            var result = await contributionRepository.AddContributionAsync(contribution, cancellationToken);

            if (!result.IsSuccess)
                return Result<Contribution>.Failure(result.Error);

            contribution.Id = result.Value;
            return Result<Contribution>.Success(contribution);
        }
        catch (ArgumentException ex)
        {
            return Result<Contribution>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateContributionAsync(UpdateContributionCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result.Failure("Update contribution command cannot be null.");

        var contributor = await contributionContributorRepository.GetContributionContributorByIdAsync(command.ContributionContributorId, cancellationToken);
        if (contributor is null)
            return Result.Failure($"Contribution contributor with ID '{command.ContributionContributorId}' not found.");

        var account = await accountRepository.GetAccountByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
            return Result.Failure($"Account with ID '{command.AccountId}' not found.");

        try
        {
            var contribution = new Contribution(command.ContributionId, command.Date, command.Amount, contributor, command.AccountId);
            return await contributionRepository.UpdateContributionAsync(contribution, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteContributionAsync(int contributionId, CancellationToken cancellationToken = default)
        => await contributionRepository.DeleteContributionAsync(contributionId, cancellationToken);
}