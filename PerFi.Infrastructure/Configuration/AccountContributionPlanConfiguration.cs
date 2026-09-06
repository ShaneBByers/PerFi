using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class AccountContributionPlanConfiguration : IEntityTypeConfiguration<AccountContributionPlanEntity>
{
    public void Configure(EntityTypeBuilder<AccountContributionPlanEntity> entity)
    {
        entity.Property(plan => plan.DollarAmountPerPayCycle).HasColumnType("decimal(18,2)");
        entity.Property(plan => plan.DollarAmountPerPayCycleAnnualIncrease).HasColumnType("decimal(18,2)");
        entity.Property(plan => plan.DollarAmountAnnual).HasColumnType("decimal(18,2)");
        entity.Property(plan => plan.DollarAmountAnnualIncrease).HasColumnType("decimal(18,2)");
        entity.Property(plan => plan.PercentagePerPayCycle).HasColumnType("decimal(18,4)");
        entity.Property(plan => plan.PercentagePerPayCycleAnnualIncrease).HasColumnType("decimal(18,4)");
        entity.Property(plan => plan.PercentageAnnual).HasColumnType("decimal(18,4)");
        entity.Property(plan => plan.PercentageAnnualIncrease).HasColumnType("decimal(18,4)");

        entity.HasIndex(plan => new { plan.AccountId, plan.ContributorType, plan.EffectiveDate })
            .IsUnique();

        entity.HasOne(plan => plan.Account)
            .WithMany(account => account.ContributionPlans)
            .HasForeignKey(plan => plan.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
