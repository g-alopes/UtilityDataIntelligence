using System.Net.Http.Json;
using System.Text.Json;
using UtilityData.Web.Models;
namespace UtilityData.Web.Services;
public sealed class UiAuthService(IHttpClientFactory factory, UiAuthSession session)
{
    public async Task LoginAsync(string email, string password)
    {
        using var response = await factory.CreateClient("Api").PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Login failed ({(int)response.StatusCode}): {body}");
        var result = JsonSerializer.Deserialize<AuthResponse>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Empty login response.");
        if (string.IsNullOrWhiteSpace(result.AccessToken)) throw new InvalidOperationException("Login response did not contain accessToken.");
        session.SignIn(result.AccessToken, email, result.Role);
    }
    public async Task RegisterAsync(RegisterRequest request)
    {
        using var response = await factory.CreateClient("Api").PostAsJsonAsync("api/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Registration failed ({(int)response.StatusCode}): {body}");
    }
}
