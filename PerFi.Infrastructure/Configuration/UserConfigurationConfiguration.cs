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

        entity.Property(configuration => configuration.CurrentAnnualSalary)
            .HasColumnType("decimal(18,2)");

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(configuration => configuration.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        entity.HasOne(configuration => configuration.UserExpectations)
            .WithOne()
            .HasForeignKey<UserConfigurationExpectationsEntity>(expectations => expectations.UserId)
            .HasPrincipalKey<UserConfigurationEntity>(configuration => configuration.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}