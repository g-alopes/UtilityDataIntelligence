namespace UtilityData.Domain.Entities;

public enum ImportFileFormat
{
    Csv = 1,
    Xlsx = 2
}

public enum ImportMappingTargetKind
{
    CoreField = 1,
    TenantCustomField = 2
}

/// <summary>
/// A tenant-owned reusable import mapping. Mapping definitions are versioned;
/// published versions are immutable and are retained for import auditability.
/// </summary>
public sealed class ImportMapping
{
    private ImportMapping() { }

    public ImportMapping(Guid tenantId, string name, string? description, ImportFileFormat fileFormat)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Mapping name is required.", nameof(name));
        if (!Enum.IsDefined(fileFormat)) throw new ArgumentOutOfRangeException(nameof(fileFormat));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        FileFormat = fileFormat;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ImportFileFormat FileFormat { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Mapping name is required.", nameof(name));
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
