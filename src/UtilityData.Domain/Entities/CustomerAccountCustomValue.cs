namespace UtilityData.Domain.Entities;

/// <summary>
/// Stores exactly one typed value for a customer account and tenant-defined field.
/// Only the column matching TenantCustomField.DataType should be populated.
/// </summary>
public sealed class CustomerAccountCustomValue
{
    private CustomerAccountCustomValue() { }

    public CustomerAccountCustomValue(
        Guid tenantId,
        Guid customerAccountId,
        Guid tenantCustomFieldId,
        string? textValue = null,
        long? integerValue = null,
        decimal? decimalValue = null,
        DateTime? dateValue = null,
        bool? booleanValue = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (customerAccountId == Guid.Empty) throw new ArgumentException("Customer account ID is required.", nameof(customerAccountId));
        if (tenantCustomFieldId == Guid.Empty) throw new ArgumentException("Custom field ID is required.", nameof(tenantCustomFieldId));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        CustomerAccountId = customerAccountId;
        TenantCustomFieldId = tenantCustomFieldId;
        SetValues(textValue, integerValue, decimalValue, dateValue, booleanValue);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CustomerAccountId { get; private set; }
    public Guid TenantCustomFieldId { get; private set; }
    public string? TextValue { get; private set; }
    public long? IntegerValue { get; private set; }
    public decimal? DecimalValue { get; private set; }
    public DateTime? DateValue { get; private set; }
    public bool? BooleanValue { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void SetValues(
        string? textValue,
        long? integerValue,
        decimal? decimalValue,
        DateTime? dateValue,
        bool? booleanValue)
    {
        var populated = (textValue is not null ? 1 : 0)
            + (integerValue.HasValue ? 1 : 0)
            + (decimalValue.HasValue ? 1 : 0)
            + (dateValue.HasValue ? 1 : 0)
            + (booleanValue.HasValue ? 1 : 0);

        if (populated != 1)
            throw new ArgumentException("Exactly one typed value must be supplied.");

        TextValue = textValue;
        IntegerValue = integerValue;
        DecimalValue = decimalValue;
        DateValue = dateValue;
        BooleanValue = booleanValue;
    }
}
