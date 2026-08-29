using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class AccountTypeGroupConfiguration : IEntityTypeConfiguration<AccountTypeGroupEntity>
{
    public void Configure(EntityTypeBuilder<AccountTypeGroupEntity> entity)
    {
        entity.Property(group => group.Name)
            .HasMaxLength(200);

        // Restrict (not Cascade) to avoid multiple cascade paths converging on AccountBalances
        // via AspNetUsers -> FinanceSnapshots -> AccountBalances and AspNetUsers -> AccountTypeGroups -> AccountTypes -> Accounts -> AccountBalances.
        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(group => group.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
