using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UtilityData.Web.Models;

namespace UtilityData.Web.Services;

public sealed class ImportMappingConfigurationApiService(IHttpClientFactory factory, UiAuthSession session)
{
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);

    private HttpClient Client()
    {
        var client = factory.CreateClient("Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<MappingConfigurationDto?> GetAsync(Guid id)
    {
        using var response = await Client().GetAsync($"api/import-mappings/{id}/configuration");
        await EnsureAsync(response);
        return await response.Content.ReadFromJsonAsync<MappingConfigurationDto>(json);
    }

    public async Task<FilePreviewDto?> PreviewAsync(Stream stream, string fileName, int headerRow, string? worksheet)
    {
        using var content = new MultipartFormDataContent();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(
            Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv");
        content.Add(file, "file", fileName);
        content.Add(new StringContent(headerRow.ToString()), "headerRowNumber");
        if (!string.IsNullOrWhiteSpace(worksheet)) content.Add(new StringContent(worksheet), "worksheetName");
        using var response = await Client().PostAsync("api/import-mappings/preview", content);
        await EnsureAsync(response);
        return await response.Content.ReadFromJsonAsync<FilePreviewDto>(json);
    }

    public async Task<SaveMappingVersionResponse?> SaveVersionAsync(Guid id, SaveMappingVersionRequest request)
    {
        using var response = await Client().PostAsJsonAsync($"api/import-mappings/{id}/versions", request, json);
        await EnsureAsync(response);
        return await response.Content.ReadFromJsonAsync<SaveMappingVersionResponse>(json);
    }

    private static async Task EnsureAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new ApiException((int)response.StatusCode, ExtractMessage(body), "Import mapping request failed");
    }

    private static string ExtractMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString()!;
            if (document.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return message.GetString()!;
            if (document.RootElement.TryGetProperty("errors", out var errors))
                return string.Join(" ", errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString()));
        }
        catch (JsonException) { }
        return string.IsNullOrWhiteSpace(body) ? "The API request failed." : body;
    }
}
