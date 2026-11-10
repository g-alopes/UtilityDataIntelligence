namespace UtilityData.Domain.Entities;

/// <summary>Typed custom-field value staged for one imported row until batch approval.</summary>
public sealed class ImportRecordCustomValue
{
    private ImportRecordCustomValue() { }
    public ImportRecordCustomValue(Guid tenantId, Guid importRecordId, Guid tenantCustomFieldId,
        string? textValue = null, long? integerValue = null, decimal? decimalValue = null,
        DateTime? dateValue = null, bool? booleanValue = null)
    {
        Id = Guid.NewGuid(); TenantId = tenantId; ImportRecordId = importRecordId;
        TenantCustomFieldId = tenantCustomFieldId;
        var count = (textValue is null ? 0 : 1) + (integerValue.HasValue ? 1 : 0) +
            (decimalValue.HasValue ? 1 : 0) + (dateValue.HasValue ? 1 : 0) + (booleanValue.HasValue ? 1 : 0);
        if (count != 1) throw new ArgumentException("Exactly one typed value must be supplied.");
        TextValue = textValue; IntegerValue = integerValue; DecimalValue = decimalValue;
        DateValue = dateValue; BooleanValue = booleanValue; CreatedAtUtc = DateTime.UtcNow;
    }
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ImportRecordId { get; private set; }
    public Guid TenantCustomFieldId { get; private set; }
    public string? TextValue { get; private set; }
    public long? IntegerValue { get; private set; }
    public decimal? DecimalValue { get; private set; }
    public DateTime? DateValue { get; private set; }
    public bool? BooleanValue { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
