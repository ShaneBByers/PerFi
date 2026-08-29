using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class FinanceSnapshotConfiguration : IEntityTypeConfiguration<FinanceSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<FinanceSnapshotEntity> entity)
    {
        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(snapshot => snapshot.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
