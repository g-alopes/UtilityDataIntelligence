
using System.Security.Claims;
using UtilityData.Web.Models;

namespace UtilityData.Web.Services;

public sealed class UiAuthSession
{
    public string? AccessToken { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }

    public Guid? TenantId { get; private set; }
    public string? TenantName { get; private set; }
    public string? TenantCode { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken);

    public event Action? Changed;

    public void RestoreFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            SignOut();
            return;
        }

        AccessToken = principal.FindFirstValue("access_token");
        Email = principal.FindFirstValue(ClaimTypes.Email);
        Role = principal.FindFirstValue(ClaimTypes.Role);

        var tenantId = principal.FindFirstValue("tenant_id");

        TenantId = Guid.TryParse(tenantId, out var id)
            ? id
            : null;

        TenantName = principal.FindFirstValue("tenant_name");
        TenantCode = principal.FindFirstValue("tenant_code");

        Changed?.Invoke();
    }

    public void SetTenantInfo(TenantInfoDto tenant)
    {
        TenantId = tenant.TenantId;
        TenantName = tenant.TenantName;
        TenantCode = tenant.TenantCode;
        Email = tenant.Email ?? Email;
        Role = tenant.Role;

        Changed?.Invoke();
    }

    public void SignOut()
    {
        AccessToken = null;
        Email = null;
        Role = null;
        TenantId = null;
        TenantName = null;
        TenantCode = null;

        Changed?.Invoke();
    }
}
