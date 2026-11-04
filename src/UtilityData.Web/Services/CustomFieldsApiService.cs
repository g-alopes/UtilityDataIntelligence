using UtilityData.Web.Models;

namespace UtilityData.Web.Services;

public sealed class CustomFieldsApiService(UtilityApiClient api)
{
    public async Task<IReadOnlyList<CustomFieldDto>> GetAllAsync()
    {
        return await api.GetAsync<List<CustomFieldDto>>("api/custom-fields/")
            ?? [];
    }

    public Task<CustomFieldDto?> GetAsync(Guid id) =>
        api.GetAsync<CustomFieldDto>($"api/custom-fields/{id}");

    public Task<CustomFieldDto?> CreateAsync(CreateCustomFieldRequest request) =>
        api.PostJsonAsync<CreateCustomFieldRequest, CustomFieldDto>(
            "api/custom-fields/", request);

    public Task<CustomFieldDto?> UpdateAsync(
        Guid id, UpdateCustomFieldRequest request) =>
        api.PutJsonAsync<UpdateCustomFieldRequest, CustomFieldDto>(
            $"api/custom-fields/{id}", request);

    public Task<CustomFieldDto?> ActivateAsync(Guid id) =>
        api.PostJsonAsync<object, CustomFieldDto>(
            $"api/custom-fields/{id}/activate", new { });

    public Task<CustomFieldDto?> DeactivateAsync(Guid id) =>
        api.PostJsonAsync<object, CustomFieldDto>(
            $"api/custom-fields/{id}/deactivate", new { });
}
