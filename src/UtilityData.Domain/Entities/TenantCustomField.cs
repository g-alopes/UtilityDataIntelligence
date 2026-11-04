namespace UtilityData.Domain.Entities;

public enum CustomFieldDataType
{
    Text = 1,
    Integer = 2,
    Decimal = 3,
    Date = 4,
    Boolean = 5
}

public sealed class TenantCustomField
{
    private TenantCustomField() { }

    public TenantCustomField(
        Guid tenantId,
        string name,
        string label,
        CustomFieldDataType dataType,
        bool isRequired = false)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Field name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Field label is required.", nameof(label));
        if (!Enum.IsDefined(dataType)) throw new ArgumentOutOfRangeException(nameof(dataType));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = NormalizeName(name);
        Label = label.Trim();
        DataType = dataType;
        IsRequired = isRequired;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public CustomFieldDataType DataType { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void Update(string label, bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Field label is required.", nameof(label));
        Label = label.Trim();
        IsRequired = isRequired;
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

    private static string NormalizeName(string name)
    {
        var value = name.Trim();
        if (value.Length > 80) throw new ArgumentException("Field name cannot exceed 80 characters.", nameof(name));
        if (!char.IsLetter(value[0]) && value[0] != '_')
            throw new ArgumentException("Field name must start with a letter or underscore.", nameof(name));
        if (value.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            throw new ArgumentException("Field name can contain only letters, digits and underscores.", nameof(name));
        return value;
    }
}
