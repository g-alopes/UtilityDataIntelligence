using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UtilityData.Domain.Entities;

namespace UtilityData.Infrastructure.Persistence.Configurations;

internal sealed class CustomerAccountCustomValueConfiguration : IEntityTypeConfiguration<CustomerAccountCustomValue>
{
    public void Configure(EntityTypeBuilder<CustomerAccountCustomValue> b)
    {
        b.ToTable("CustomerAccountCustomValues", table =>
        {
            table.HasCheckConstraint(
                "CK_CustomerAccountCustomValues_ExactlyOneValue",
                "((CASE WHEN [TextValue] IS NULL THEN 0 ELSE 1 END) + " +
                "(CASE WHEN [IntegerValue] IS NULL THEN 0 ELSE 1 END) + " +
                "(CASE WHEN [DecimalValue] IS NULL THEN 0 ELSE 1 END) + " +
                "(CASE WHEN [DateValue] IS NULL THEN 0 ELSE 1 END) + " +
                "(CASE WHEN [BooleanValue] IS NULL THEN 0 ELSE 1 END)) = 1");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.TextValue).HasMaxLength(4000);
        b.Property(x => x.DecimalValue).HasPrecision(19, 6);
        b.Property(x => x.DateValue).HasColumnType("datetime2");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.HasIndex(x => new { x.TenantId, x.CustomerAccountId, x.TenantCustomFieldId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TenantCustomFieldId });
        b.HasOne<CustomerAccount>().WithMany().HasForeignKey(x => x.CustomerAccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<TenantCustomField>().WithMany().HasForeignKey(x => x.TenantCustomFieldId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
