namespace UtilityData.Web.Models;

public enum MappingTargetKind { CoreField = 1, TenantCustomField = 2 }

public sealed record MappingConfigurationDto(
    Guid Id, string Name, string? Description, ImportFileFormat FileFormat, bool IsActive,
    Guid CurrentVersionId, int CurrentVersionNumber, string? WorksheetName, int HeaderRowNumber,
    string? DateFormat, string? DecimalSeparator, string? ThousandsSeparator,
    IReadOnlyList<MappingColumnDto> Columns, IReadOnlyList<MappingCustomFieldDto> CustomFields);

public sealed record MappingColumnDto(Guid Id, string SourceColumnName, MappingTargetKind TargetKind,
    string? TargetFieldKey, Guid? TenantCustomFieldId, bool IsRequired);

public sealed record MappingCustomFieldDto(Guid Id, string Name, string Label, int DataType, bool IsRequired);

public sealed record FilePreviewDto(string FileName, string FileFormat, string? WorksheetName,
    IReadOnlyList<string> Headers, IReadOnlyList<string>? Worksheets);

public sealed record SaveMappingVersionRequest(string? WorksheetName, int HeaderRowNumber,
    string? DateFormat, string DecimalSeparator, string? ThousandsSeparator,
    IReadOnlyList<SaveMappingColumnRequest> Columns);

public sealed record SaveMappingColumnRequest(string SourceColumnName, MappingTargetKind TargetKind,
    string? TargetFieldKey, Guid? TenantCustomFieldId, bool IsRequired);

public sealed record SaveMappingVersionResponse(Guid VersionId, int VersionNumber, string Message);
