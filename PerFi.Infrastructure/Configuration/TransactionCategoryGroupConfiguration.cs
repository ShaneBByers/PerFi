using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class TransactionCategoryGroupConfiguration : IEntityTypeConfiguration<TransactionCategoryGroupEntity>
{
    public void Configure(EntityTypeBuilder<TransactionCategoryGroupEntity> entity)
    {
        entity.Property(group => group.Name)
            .HasMaxLength(200);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(group => group.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
