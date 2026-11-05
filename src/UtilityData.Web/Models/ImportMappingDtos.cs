namespace UtilityData.Web.Models;

public enum ImportFileFormat
{
    Csv = 1,
    Xlsx = 2
}

public sealed record ImportMappingDto(
    Guid Id,
    string Name,
    string? Description,
    ImportFileFormat FileFormat,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int LatestVersionNumber,
    int VersionCount);

public sealed record CreateImportMappingRequest(
    string Name,
    string? Description,
    ImportFileFormat FileFormat);

public sealed record UpdateImportMappingRequest(
    string Name,
    string? Description);

public sealed record ImportMappingDetailDto(
    Guid Id,
    string Name,
    string? Description,
    ImportFileFormat FileFormat,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<ImportMappingVersionDto> Versions);

public sealed record ImportMappingVersionDto(
    Guid Id,
    int VersionNumber,
    string? WorksheetName,
    int HeaderRowNumber,
    string? DateFormat,
    string? DecimalSeparator,
    string? ThousandsSeparator,
    DateTime CreatedAtUtc,
    int ColumnCount);
