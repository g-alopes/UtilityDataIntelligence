using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UtilityData.Domain.Entities;

namespace UtilityData.Infrastructure.Persistence.Configurations;

internal sealed class ImportMappingVersionConfiguration : IEntityTypeConfiguration<ImportMappingVersion>
{
    public void Configure(EntityTypeBuilder<ImportMappingVersion> b)
    {
        b.ToTable("ImportMappingVersions");
        b.HasKey(x => x.Id);
        b.Property(x => x.WorksheetName).HasMaxLength(128);
        b.Property(x => x.DateFormat).HasMaxLength(80);
        b.Property(x => x.DecimalSeparator).HasMaxLength(1);
        b.Property(x => x.ThousandsSeparator).HasMaxLength(1);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.HasIndex(x => new { x.TenantId, x.ImportMappingId, x.VersionNumber }).IsUnique();
        b.HasOne<ImportMapping>().WithMany().HasForeignKey(x => x.ImportMappingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
