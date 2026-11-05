using UtilityData.Web.Models;

namespace UtilityData.Web.Services;

public sealed class ImportMappingsApiService(UtilityApiClient api)
{
    public async Task<IReadOnlyList<ImportMappingDto>> GetAllAsync() =>
        await api.GetAsync<List<ImportMappingDto>>("api/import-mappings/") ?? [];

    public Task<ImportMappingDetailDto?> GetAsync(Guid id) =>
        api.GetAsync<ImportMappingDetailDto>($"api/import-mappings/{id}");

    public Task<ImportMappingDto?> CreateAsync(CreateImportMappingRequest request) =>
        api.PostJsonAsync<CreateImportMappingRequest, ImportMappingDto>(
            "api/import-mappings/", request);

    public Task<ImportMappingDto?> UpdateAsync(Guid id, UpdateImportMappingRequest request) =>
        api.PutJsonAsync<UpdateImportMappingRequest, ImportMappingDto>(
            $"api/import-mappings/{id}", request);

    public Task<ImportMappingDto?> ActivateAsync(Guid id) =>
        api.PostJsonAsync<object, ImportMappingDto>(
            $"api/import-mappings/{id}/activate", new { });

    public Task<ImportMappingDto?> DeactivateAsync(Guid id) =>
        api.PostJsonAsync<object, ImportMappingDto>(
            $"api/import-mappings/{id}/deactivate", new { });
}
