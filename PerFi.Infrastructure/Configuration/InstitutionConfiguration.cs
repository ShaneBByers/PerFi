using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class InstitutionConfiguration : IEntityTypeConfiguration<InstitutionEntity>
{
    public void Configure(EntityTypeBuilder<InstitutionEntity> entity)
    {
        entity.Property(institution => institution.Name)
            .HasMaxLength(200);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(institution => institution.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
