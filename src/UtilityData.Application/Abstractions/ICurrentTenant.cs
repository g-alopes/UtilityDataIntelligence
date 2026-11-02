namespace UtilityData.Application.Abstractions;
/// <summary>Tenant identity resolved by the server from a validated authenticated principal.</summary>
public interface ICurrentTenant
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string Role { get; }
}
