using UtilityData.Web.Models;

namespace UtilityData.Web.Services;

public sealed class UiAuthService(IHttpClientFactory factory)
{
    public async Task RegisterAsync(RegisterRequest request)
    {
        using var response = await factory
            .CreateClient("Api")
            .PostAsJsonAsync("api/auth/register", request);

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();

        throw new InvalidOperationException(
            $"Registration failed ({(int)response.StatusCode}): {body}");
    }
}
