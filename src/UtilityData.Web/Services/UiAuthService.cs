using System.Text.Json;
using UtilityData.Web.Models;

namespace UtilityData.Web.Services;

public sealed class UiAuthService(
    IHttpClientFactory factory,
    UiAuthSession session,
    UtilityApiClient api)
{
    private readonly JsonSerializerOptions json =
        new(JsonSerializerDefaults.Web);

    public async Task LoginAsync(string email, string password)
    {
        using var response = await factory
            .CreateClient("Api")
            .PostAsJsonAsync(
                "api/auth/login",
                new LoginRequest(email, password));

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Login failed ({(int)response.StatusCode}): {body}");
        }

        var result = JsonSerializer.Deserialize<AuthResponse>(
            body, json)
            ?? throw new InvalidOperationException("Empty login response.");

        if (string.IsNullOrWhiteSpace(result.AccessToken))
        {
            throw new InvalidOperationException(
                "Login response did not contain accessToken.");
        }

        session.SignIn(result.AccessToken, email, result.Role);

        try
        {
            var tenant = await api.GetAsync<TenantInfoDto>("api/tenant/me");

            if (tenant is null)
                throw new InvalidOperationException(
                    "The API did not return tenant information.");

            session.SetTenantInfo(tenant);
        }
        catch
        {
            session.SignOut();
            throw;
        }
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        using var response = await factory
            .CreateClient("Api")
            .PostAsJsonAsync("api/auth/register", request);

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Registration failed ({(int)response.StatusCode}): {body}");
        }
    }

    public void Logout()
    {
        session.SignOut();
    }
}