using Microsoft.EntityFrameworkCore;
using PerFi.Infrastructure;

namespace PerFi.Console.Operations;

public class FetchInstitutionsOperation(PerFiDbContext dbContext)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var institutions = await dbContext.Institutions
            .AsNoTracking()
            .OrderBy(institution => institution.Id)
            .Select(institution => new
            {
                institution.Id,
                institution.Name
            })
            .Take(10)
            .ToListAsync(cancellationToken);

        System.Console.WriteLine($"Found {institutions.Count} institution rows (showing up to 10).");

        foreach (var institution in institutions)
        {
            System.Console.WriteLine($"Institution Id={institution.Id}, Name={institution.Name}");
        }
    }
}
