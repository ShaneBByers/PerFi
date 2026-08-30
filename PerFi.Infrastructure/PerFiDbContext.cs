using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure;

public class PerFiDbContext(DbContextOptions<PerFiDbContext> options)
   : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<AccountTypeGroupEntity> AccountTypeGroups { get; set; }
    public DbSet<InstitutionEntity> Institutions { get; set; }
    public DbSet<AccountTypeEntity> AccountTypes { get; set; }
    public DbSet<AccountEntity> Accounts { get; set; }
    public DbSet<AccountBalanceEntity> AccountBalances { get; set; }
    public DbSet<FinanceSnapshotEntity> FinanceSnapshots { get; set; }
    public DbSet<TransactionCategoryGroupEntity> TransactionCategoryGroups { get; set; }
    public DbSet<TransactionCategoryEntity> TransactionCategories { get; set; }
    public DbSet<TransactionEntity> Transactions { get; set; }
    public DbSet<ContributionContributorEntity> ContributionContributors { get; set; }
    public DbSet<ContributionEntity> Contributions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PerFiDbContext).Assembly);
    }
}
