using UtilityData.Domain.Entities;

namespace UtilityData.Web.Models;
public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string TenantName, string TenantCode, string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, string? UserId = null, string? TenantId = null, string? Role = null);
public sealed record DashboardSummaryDto(int CustomerAccountCount, int ImportBatchCount, int PendingApprovalCount, int InvalidRecordCount);
public sealed record CustomerAccountDto(Guid Id, string AccountNumber, string Name, string? Email, DateTime CreatedAtUtc);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Skip, int Take);
public sealed record ImportBatchDto(Guid Id, string FileName, string Status, int TotalRecordCount, int ValidRecordCount, int InvalidRecordCount, DateTime CreatedAtUtc);
public sealed record ImportRecordDto(
    int RowNumber,
    string? AccountNumber,
    string? CustomerName,
    string? Email,
    string? ServiceAddress,
    bool IsValid,
    string? Errors);
public sealed record ImportRecordsResponse(
    Guid BatchId,
    int Skip,
    int Take,
    IReadOnlyList<ImportRecordDto> Records);
public sealed record ImportBatchDetailDto(
    Guid Id,
    string FileName,
    ImportBatchStatus Status,
    int TotalRecords,
    int ValidRecords,
    int InvalidRecords,
    int ApprovedRecordCount,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    string? ApprovedByUserId);
