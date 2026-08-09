namespace PerFi.Application.Commands;

public record UpdateInstitutionCommand(
    int InstitutionId,
    string InstitutionName);