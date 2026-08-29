using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class AccountBalanceConfiguration : IEntityTypeConfiguration<AccountBalanceEntity>
{
    public void Configure(EntityTypeBuilder<AccountBalanceEntity> entity)
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

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(balance => balance.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
