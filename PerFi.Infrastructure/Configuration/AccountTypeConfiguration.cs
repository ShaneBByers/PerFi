using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class AccountTypeConfiguration : IEntityTypeConfiguration<AccountTypeEntity>
{
    public void Configure(EntityTypeBuilder<AccountTypeEntity> entity)
    {
        entity.Property(accountType => accountType.Name)
            .HasMaxLength(200);

        entity.HasOne(accountType => accountType.AccountTypeGroup)
            .WithMany(group => group.AccountTypes)
            .HasForeignKey(accountType => accountType.AccountTypeGroupId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(accountType => accountType.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
