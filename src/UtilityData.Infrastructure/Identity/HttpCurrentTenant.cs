using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using UtilityData.Application.Abstractions;
namespace UtilityData.Infrastructure.Identity;
public sealed class HttpCurrentTenant(IHttpContextAccessor accessor) : ICurrentTenant
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("No active HTTP request.");
    public Guid TenantId => ReadGuid("tenant_id");
    public Guid UserId => ReadGuid(ClaimTypes.NameIdentifier);
    public string Role => Principal.FindFirstValue(ClaimTypes.Role)
        ?? throw new UnauthorizedAccessException("Missing role claim.");
    private Guid ReadGuid(string type)
    {
        var value = Principal.FindFirstValue(type);
        return Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id : throw new UnauthorizedAccessException($"Missing or invalid claim: {type}");
    }
}
