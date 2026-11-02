using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UtilityData.Domain.Entities;

namespace UtilityData.Infrastructure.Persistence.Configurations;

internal sealed class CustomerAccountConfiguration : IEntityTypeConfiguration<CustomerAccount>
{
    public void Configure(EntityTypeBuilder<CustomerAccount> builder)
    {
        builder.ToTable("CustomerAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountNumber).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.ServiceAddress).HasMaxLength(500);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");

        // Tenant-scoped uniqueness and access paths.
        builder.HasIndex(x => new { x.TenantId, x.AccountNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
