namespace UtilityData.Web.Services;
public sealed class UiAuthSession
{
    public string? AccessToken { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);
    public void SignIn(string token, string email, string? role) { AccessToken = token; Email = email; Role = role; }
    public void SignOut() { AccessToken = null; Email = null; Role = null; }
}
