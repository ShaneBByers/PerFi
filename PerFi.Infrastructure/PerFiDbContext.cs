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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.HasOne(account => account.AccountType)
                .WithMany(accountType => accountType.Accounts)
                .HasForeignKey(account => account.AccountTypeId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne(account => account.Institution)
                .WithMany(institution => institution.Accounts)
                .HasForeignKey(account => account.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity<AccountTypeEntity>(entity =>
        {
            entity.HasOne(accountType => accountType.AccountTypeGroup)
                .WithMany(group => group.AccountTypes)
                .HasForeignKey(accountType => accountType.AccountTypeGroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity<AccountBalanceEntity>(entity =>
        {
            entity.Property(balance => balance.Balance)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(balance => balance.Account)
                .WithMany()
                .HasForeignKey(balance => balance.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne(balance => balance.FinanceSnapshot)
                .WithMany(snapshot => snapshot.AccountBalances)
                .HasForeignKey(balance => balance.FinanceSnapshotId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity<InstitutionEntity>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(institution => institution.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity<AccountTypeGroupEntity>(entity =>
        {
            // Restrict (not Cascade) to avoid multiple cascade paths converging on AccountBalances
            // via AspNetUsers -> FinanceSnapshots -> AccountBalances and AspNetUsers -> AccountTypeGroups -> AccountTypes -> Accounts -> AccountBalances.
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(group => group.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity<FinanceSnapshotEntity>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }
}
