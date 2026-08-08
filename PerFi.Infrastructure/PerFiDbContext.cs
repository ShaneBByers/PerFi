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

        modelBuilder.Entity<InstitutionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Accounts)
                .WithOne()
                .HasForeignKey("InstitutionId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountTypeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasOne(e => e.Type)
                .WithMany()
                .HasForeignKey("AccountTypeId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinanceSnapshotEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();
            entity.HasMany(e => e.AccountBalances)
                .WithOne()
                .HasForeignKey("FinanceSnapshotId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountBalanceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Balance).HasColumnType("decimal(18,2)").IsRequired();
            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
