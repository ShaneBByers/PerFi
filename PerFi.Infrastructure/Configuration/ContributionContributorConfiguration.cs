using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class ContributionContributorConfiguration : IEntityTypeConfiguration<ContributionContributorEntity>
{
    public void Configure(EntityTypeBuilder<ContributionContributorEntity> entity)
    {
        entity.Property(contributor => contributor.Name)
            .HasMaxLength(200);

        entity.HasMany(contributor => contributor.Contributions)
            .WithOne(contribution => contribution.Contributor)
            .HasForeignKey(contribution => contribution.ContributorId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(contributor => contributor.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
