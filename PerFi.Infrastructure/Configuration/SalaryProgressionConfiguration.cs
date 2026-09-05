using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class SalaryProgressionConfiguration : IEntityTypeConfiguration<SalaryProgressionEntity>
{
    public void Configure(EntityTypeBuilder<SalaryProgressionEntity> entity)
    {
        entity.Property(salaryProgression => salaryProgression.AnnualSalary)
            .HasColumnType("decimal(18,2)");

        entity.HasIndex(salaryProgression => new { salaryProgression.UserId, salaryProgression.EffectiveDate })
            .IsUnique();

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(salaryProgression => salaryProgression.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
