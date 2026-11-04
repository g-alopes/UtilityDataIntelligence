namespace UtilityData.Domain.Entities;

/// <summary>
/// Immutable snapshot of one mapping configuration. Create a new version when
/// changing columns, worksheet, header row, or parsing settings.
/// </summary>
public sealed class ImportMappingVersion
{
    private ImportMappingVersion() { }

    public ImportMappingVersion(
        Guid tenantId,
        Guid importMappingId,
        int versionNumber,
        string? worksheetName,
        int headerRowNumber,
        string? dateFormat,
        string? decimalSeparator,
        string? thousandsSeparator)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (importMappingId == Guid.Empty) throw new ArgumentException("Mapping ID is required.", nameof(importMappingId));
        if (versionNumber < 1) throw new ArgumentOutOfRangeException(nameof(versionNumber));
        if (headerRowNumber < 1) throw new ArgumentOutOfRangeException(nameof(headerRowNumber));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        ImportMappingId = importMappingId;
        VersionNumber = versionNumber;
        WorksheetName = string.IsNullOrWhiteSpace(worksheetName) ? null : worksheetName.Trim();
        HeaderRowNumber = headerRowNumber;
        DateFormat = string.IsNullOrWhiteSpace(dateFormat) ? null : dateFormat.Trim();
        DecimalSeparator = decimalSeparator;
        ThousandsSeparator = thousandsSeparator;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ImportMappingId { get; private set; }
    public int VersionNumber { get; private set; }
    public string? WorksheetName { get; private set; }
    public int HeaderRowNumber { get; private set; }
    public string? DateFormat { get; private set; }
    public string? DecimalSeparator { get; private set; }
    public string? ThousandsSeparator { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
