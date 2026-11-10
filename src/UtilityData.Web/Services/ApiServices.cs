using UtilityData.Web.Models;

namespace UtilityData.Web.Services;

public sealed class DashboardApiService(UtilityApiClient api) { public Task<DashboardSummaryDto?> GetSummaryAsync() => api.GetAsync<DashboardSummaryDto>("api/dashboard/summary"); }
public sealed class CustomerAccountsApiService(UtilityApiClient api) { public Task<PagedResult<CustomerAccountDto>?> GetAsync(int skip = 0, int take = 25) => api.GetAsync<PagedResult<CustomerAccountDto>>($"api/customer-accounts?skip={skip}&take={take}"); }
public sealed class ImportsApiService(UtilityApiClient api)
{
    public Task<PagedResult<ImportBatchDto>?> GetAsync(int skip = 0, int take = 25) => api.GetAsync<PagedResult<ImportBatchDto>>($"api/imports?skip={skip}&take={take}");
    public Task<ImportBatchDetailDto?> GetBatchAsync(Guid id) => api.GetAsync<ImportBatchDetailDto>($"api/imports/{id}");
    public Task<ImportRecordsResponse?> GetRecordsAsync(
    Guid id,
    int skip = 0,
    int take = 100) =>
    api.GetAsync<ImportRecordsResponse>(
        $"api/imports/{id}/records?skip={skip}&take={take}");
    public Task<IReadOnlyList<ImportMappingOptionDto>?> GetMappingOptionsAsync() => api.GetAsync<IReadOnlyList<ImportMappingOptionDto>>("api/imports/mapping-options");
    public Task<ImportUploadAcceptedDto?> UploadAsync(Stream stream, string name, Guid mappingVersionId) => api.UploadAsync<ImportUploadAcceptedDto>("api/imports", stream, name, mappingVersionId);
    public Task ApproveAsync(Guid id) => api.PostAsync($"api/imports/{id}/approve");
}
