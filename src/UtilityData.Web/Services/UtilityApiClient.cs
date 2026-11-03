using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace UtilityData.Web.Services;
public sealed class UtilityApiClient(IHttpClientFactory factory, UiAuthSession session)
{
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private HttpClient Client()
    {
        var client = factory.CreateClient("Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }
    public async Task<T?> GetAsync<T>(string uri)
    {
        using var response = await Client().GetAsync(uri);
        await Ensure(response);
        return await response.Content.ReadFromJsonAsync<T>(json);
    }
    public async Task<TResponse?> PostJsonAsync<TRequest,TResponse>(string uri,TRequest body)
    {
        using var response = await Client().PostAsJsonAsync(uri, body, json);
        await Ensure(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(json);
    }
    public async Task PostAsync(string uri)
    {
        using var response = await Client().PostAsync(uri, null);
        await Ensure(response);
    }
    public async Task<T?> UploadAsync<T>(string uri, Stream stream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", fileName);
        using var response = await Client().PostAsync(uri, content);
        await Ensure(response);
        return await response.Content.ReadFromJsonAsync<T>(json);
    }
    private static async Task Ensure(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"API request failed ({(int)response.StatusCode}): {body}", null, response.StatusCode);
    }
}
