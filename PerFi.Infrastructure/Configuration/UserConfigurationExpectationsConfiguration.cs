using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class UserConfigurationExpectationsConfiguration : IEntityTypeConfiguration<UserConfigurationExpectationsEntity>
{
    public void Configure(EntityTypeBuilder<UserConfigurationExpectationsEntity> entity)
    {
        entity.HasIndex(expectations => expectations.UserId)
            .IsUnique();

        entity.Property(expectations => expectations.AnnualSalaryRaisePercentage)
            .HasColumnType("decimal(18,4)");
        entity.Property(expectations => expectations.EmployerMatchPercentage)
            .HasColumnType("decimal(18,4)");
        entity.Property(expectations => expectations.EmployerProfitSharingPercentage)
            .HasColumnType("decimal(18,4)");
        entity.Property(expectations => expectations.AnnualBrokerageContributionPerPayCycleIncrease)
            .HasColumnType("decimal(18,2)");
        entity.Property(expectations => expectations.AnnualStockMarketReturnPercentage)
            .HasColumnType("decimal(18,4)");
        entity.Property(expectations => expectations.AnnualInflationPercentage)
            .HasColumnType("decimal(18,4)");

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(expectations => expectations.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}