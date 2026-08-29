using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class ContributionConfiguration : IEntityTypeConfiguration<ContributionEntity>
{
    public void Configure(EntityTypeBuilder<ContributionEntity> entity)
    {
        entity.Property(contribution => contribution.Amount)
            .HasColumnType("decimal(18,2)");

        entity.HasOne(contribution => contribution.Contributor)
            .WithMany()
            .HasForeignKey(contribution => contribution.ContributorId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(contribution => contribution.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
