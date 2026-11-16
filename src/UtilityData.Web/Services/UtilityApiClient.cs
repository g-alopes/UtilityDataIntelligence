using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

namespace UtilityData.Web.Services;

public sealed class UtilityApiClient(
    IHttpClientFactory factory,
    UiAuthSession session,
    NavigationManager navigation)
{
    private readonly JsonSerializerOptions json =
        new(JsonSerializerDefaults.Web);

    private HttpClient Client()
    {
        RedirectIfAccessTokenExpired();

        var client = factory.CreateClient("Api");

        if (!string.IsNullOrWhiteSpace(session.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    session.AccessToken);
        }

        return client;
    }

    public async Task<T?> GetAsync<T>(string uri)
    {
        using var response = await Client().GetAsync(uri);

        await Ensure(response);

        return await response.Content.ReadFromJsonAsync<T>(json);
    }

    public async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
        string uri,
        TRequest body)
    {
        using var response = await Client().PostAsJsonAsync(
            uri,
            body,
            json);

        await Ensure(response);

        return await response.Content.ReadFromJsonAsync<TResponse>(json);
    }

    public async Task PostAsync(string uri)
    {
        using var response = await Client().PostAsync(uri, null);

        await Ensure(response);
    }

    public async Task<T?> UploadAsync<T>(
        string uri,
        Stream stream,
        string fileName,
        Guid mappingVersionId)
    {
        using var content = new MultipartFormDataContent();
        using var file = new StreamContent(stream);

        var extension = Path.GetExtension(fileName);

        file.Headers.ContentType = new MediaTypeHeaderValue(
            extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/csv");

        content.Add(file, "file", fileName);
        content.Add(
            new StringContent(mappingVersionId.ToString()),
            "mappingVersionId");

        using var response = await Client().PostAsync(uri, content);

        await Ensure(response);

        return await response.Content.ReadFromJsonAsync<T>(json);
    }

    public async Task<TResponse?> PutJsonAsync<TRequest, TResponse>(
        string uri,
        TRequest body)
    {
        using var response = await Client().PutAsJsonAsync(uri, body, json);

        await Ensure(response);

        return await response.Content.ReadFromJsonAsync<TResponse>(json);
    }

    private void RedirectIfAccessTokenExpired()
    {
        var token = session.AccessToken;

        if (string.IsNullOrWhiteSpace(token) ||
            !TryGetTokenExpiration(token, out var expiresAtUtc))
        {
            return;
        }

        // Redirect slightly before the JWT expiration to avoid sending a
        // request with a token that is about to expire.
        if (DateTimeOffset.UtcNow < expiresAtUtc.AddSeconds(-30))
        {
            return;
        }

        RedirectToSignIn();
    }

    private async Task Ensure(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            RedirectToSignIn();

            throw new ApiException(
                (int)response.StatusCode,
                "Your session has expired. Redirecting to the sign-in page.");
        }

        ProblemDetails? problem = null;

        try
        {
            problem = await response.Content
                .ReadFromJsonAsync<ProblemDetails>(json);
        }
        catch (JsonException)
        {
            // Response was not a valid ProblemDetails document.
        }
        catch (NotSupportedException)
        {
            // Response content could not be deserialized.
        }

        var message = problem?.Detail
            ?? problem?.Title
            ?? $"The API request failed ({(int)response.StatusCode}).";

        throw new ApiException(
            (int)response.StatusCode,
            message,
            problem?.Title);
    }

    private void RedirectToSignIn()
    {
        navigation.NavigateTo(
            "/bff/session-expired",
            forceLoad: true);
    }

    private static bool TryGetTokenExpiration(
        string token,
        out DateTimeOffset expiresAtUtc)
    {
        expiresAtUtc = default;

        try
        {
            var parts = token.Split('.');

            if (parts.Length != 3)
            {
                return false;
            }

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            payload = payload.PadRight(
                payload.Length + ((4 - payload.Length % 4) % 4),
                '=');

            var payloadBytes = Convert.FromBase64String(payload);

            using var document = JsonDocument.Parse(payloadBytes);

            if (!document.RootElement.TryGetProperty(
                    "exp",
                    out var expiration) ||
                !expiration.TryGetInt64(out var unixSeconds))
            {
                return false;
            }

            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
