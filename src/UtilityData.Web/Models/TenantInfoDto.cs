namespace UtilityData.Web.Models;

public sealed record TenantInfoDto(
    Guid TenantId,
    Guid UserId,
    string Role,
    string? Email,
    string TenantName,
    string TenantCode,
    bool IsActive);