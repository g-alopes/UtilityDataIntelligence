using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UtilityData.Domain.Entities;

namespace UtilityData.Infrastructure.Persistence.Configurations;

internal sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> b)
    {
        b.ToTable("ImportBatches"); b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        b.Property(x => x.StoredFilePath).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.ErrorMessage).HasMaxLength(1000);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.CompletedAtUtc).HasColumnType("datetime2");
        b.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportMappingVersion>().WithMany().HasForeignKey(x => x.ImportMappingVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
