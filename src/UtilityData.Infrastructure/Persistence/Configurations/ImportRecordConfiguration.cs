using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UtilityData.Domain.Entities;

namespace UtilityData.Infrastructure.Persistence.Configurations;

internal sealed class ImportRecordConfiguration : IEntityTypeConfiguration<ImportRecord>
{
    public void Configure(EntityTypeBuilder<ImportRecord> b)
    {
        b.ToTable("ImportRecords"); b.HasKey(x => x.Id);
        b.Property(x => x.AccountNumber).HasMaxLength(80);
        b.Property(x => x.CustomerName).HasMaxLength(200);
        b.Property(x => x.Email).HasMaxLength(320);
        b.Property(x => x.ServiceAddress).HasMaxLength(500);
        b.Property(x => x.Errors).HasMaxLength(2000);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.HasIndex(x => new { x.TenantId, x.ImportBatchId, x.RowNumber }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.ImportBatchId, x.IsValid });
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
