namespace UtilityData.Web.Models;

public enum CustomFieldDataType
{
    Text = 1,
    Integer = 2,
    Decimal = 3,
    Date = 4,
    Boolean = 5
}

public sealed record CustomFieldDto(
    Guid Id,
    string Name,
    string Label,
    CustomFieldDataType DataType,
    bool IsRequired,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateCustomFieldRequest(
    string Name,
    string Label,
    CustomFieldDataType DataType,
    bool IsRequired);

public sealed record UpdateCustomFieldRequest(string Label, bool IsRequired);
