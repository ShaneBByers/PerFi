using Microsoft.EntityFrameworkCore;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure;

public class PerFiDbContext(DbContextOptions<PerFiDbContext> options)
   : DbContext(options)
{
    public DbSet<InstitutionEntity> Institutions { get; set; }
    public DbSet<AccountTypeEntity> AccountTypes { get; set; }
    public DbSet<AccountEntity> Accounts { get; set; }
    public DbSet<AccountBalanceEntity> AccountBalances { get; set; }
    public DbSet<FinanceSnapshotEntity> FinanceSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AccountEntity>()
            .HasIndex(a => a.AccountName)
            .IsUnique();
    }
}
