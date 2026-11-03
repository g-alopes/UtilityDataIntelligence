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

    public void SignIn(
        string token,
        string email,
        string? role)
    {
        AccessToken = token;
        Email = email;
        Role = role;

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