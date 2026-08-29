using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class TransactionCategoryConfiguration : IEntityTypeConfiguration<TransactionCategoryEntity>
{
    public void Configure(EntityTypeBuilder<TransactionCategoryEntity> entity)
    {
        entity.Property(category => category.Name)
            .HasMaxLength(200);

        entity.HasOne(category => category.TransactionCategoryGroup)
            .WithMany(group => group.TransactionCategories)
            .HasForeignKey(category => category.TransactionCategoryGroupId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(category => category.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
