using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class UserConfigurationConfiguration : IEntityTypeConfiguration<UserConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<UserConfigurationEntity> entity)
    {
        entity.HasIndex(configuration => configuration.UserId)
            .IsUnique();

        entity.Property(configuration => configuration.ExpectedAnnualSalaryRaisePercentage)
            .HasColumnType("decimal(18,4)");
        entity.Property(configuration => configuration.ExpectedAnnualInflationPercentage)
            .HasColumnType("decimal(18,4)");

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(configuration => configuration.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}