using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UtilityData.Domain.Entities;

namespace UtilityData.Infrastructure.Persistence.Configurations;

internal sealed class ImportMappingColumnConfiguration : IEntityTypeConfiguration<ImportMappingColumn>
{
    public void Configure(EntityTypeBuilder<ImportMappingColumn> b)
    {
        b.ToTable("ImportMappingColumns", table =>
        {
            table.HasCheckConstraint(
                "CK_ImportMappingColumns_Target",
                "(([TargetKind] = 1 AND [TargetFieldKey] IS NOT NULL AND [TenantCustomFieldId] IS NULL) OR " +
                "([TargetKind] = 2 AND [TargetFieldKey] IS NULL AND [TenantCustomFieldId] IS NOT NULL))");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.SourceColumnName).HasMaxLength(256).IsRequired();
        b.Property(x => x.TargetKind).HasConversion<int>();
        b.Property(x => x.TargetFieldKey).HasMaxLength(80);
        b.HasIndex(x => new { x.TenantId, x.ImportMappingVersionId, x.SourceColumnName }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.ImportMappingVersionId, x.TargetFieldKey });
        b.HasOne<ImportMappingVersion>().WithMany().HasForeignKey(x => x.ImportMappingVersionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<TenantCustomField>().WithMany().HasForeignKey(x => x.TenantCustomFieldId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
