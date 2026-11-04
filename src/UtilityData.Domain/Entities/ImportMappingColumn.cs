namespace UtilityData.Domain.Entities;

/// <summary>
/// Maps one incoming source header to a known core field or a tenant custom field.
/// TargetFieldKey is a stable core-field key when TargetKind is CoreField.
/// TenantCustomFieldId is populated only for TenantCustomField targets.
/// </summary>
public sealed class ImportMappingColumn
{
    private ImportMappingColumn() { }

    public ImportMappingColumn(
        Guid tenantId,
        Guid importMappingVersionId,
        string sourceColumnName,
        ImportMappingTargetKind targetKind,
        string? targetFieldKey,
        Guid? tenantCustomFieldId,
        bool isRequired)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (importMappingVersionId == Guid.Empty) throw new ArgumentException("Mapping version ID is required.", nameof(importMappingVersionId));
        if (string.IsNullOrWhiteSpace(sourceColumnName)) throw new ArgumentException("Source column name is required.", nameof(sourceColumnName));
        if (!Enum.IsDefined(targetKind)) throw new ArgumentOutOfRangeException(nameof(targetKind));

        if (targetKind == ImportMappingTargetKind.CoreField &&
            (string.IsNullOrWhiteSpace(targetFieldKey) || tenantCustomFieldId.HasValue))
            throw new ArgumentException("Core mappings require a target field key and cannot reference a custom field.");

        if (targetKind == ImportMappingTargetKind.TenantCustomField &&
            (!tenantCustomFieldId.HasValue || !string.IsNullOrWhiteSpace(targetFieldKey)))
            throw new ArgumentException("Custom mappings require a custom field ID and cannot specify a core field key.");

        Id = Guid.NewGuid();
        TenantId = tenantId;
        ImportMappingVersionId = importMappingVersionId;
        SourceColumnName = sourceColumnName.Trim();
        TargetKind = targetKind;
        TargetFieldKey = string.IsNullOrWhiteSpace(targetFieldKey) ? null : targetFieldKey.Trim();
        TenantCustomFieldId = tenantCustomFieldId;
        IsRequired = isRequired;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ImportMappingVersionId { get; private set; }
    public string SourceColumnName { get; private set; } = string.Empty;
    public ImportMappingTargetKind TargetKind { get; private set; }
    public string? TargetFieldKey { get; private set; }
    public Guid? TenantCustomFieldId { get; private set; }
    public bool IsRequired { get; private set; }
}
