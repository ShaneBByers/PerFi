using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerFi.Infrastructure.Entities;

namespace PerFi.Infrastructure.Configuration;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> entity)
    {
        entity.Property(account => account.Name)
            .HasMaxLength(200);

        entity.HasOne(account => account.AccountType)
            .WithMany(accountType => accountType.Accounts)
            .HasForeignKey(account => account.AccountTypeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        entity.HasOne(account => account.Institution)
            .WithMany(institution => institution.Accounts)
            .HasForeignKey(account => account.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        entity.HasMany(account => account.AccountBalances)
            .WithOne(balance => balance.Account)
            .HasForeignKey(balance => balance.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(account => account.Transactions)
            .WithOne(transaction => transaction.Account)
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(account => account.Contributions)
            .WithOne(contribution => contribution.Account)
            .HasForeignKey(contribution => contribution.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(account => account.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
