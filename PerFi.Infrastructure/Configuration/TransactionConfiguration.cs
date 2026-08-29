using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<TransactionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionEntity> entity)
    {
        entity.Property(transaction => transaction.Amount)
            .HasColumnType("decimal(18,2)");

        entity.Property(transaction => transaction.Description)
            .HasMaxLength(200);

        entity.HasOne(transaction => transaction.TransactionCategory)
            .WithMany(category => category.Transactions)
            .HasForeignKey(transaction => transaction.TransactionCategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        entity.HasOne(transaction => transaction.Account)
            .WithMany()
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
