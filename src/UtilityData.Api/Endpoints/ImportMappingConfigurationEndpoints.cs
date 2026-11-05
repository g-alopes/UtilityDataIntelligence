using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using ClosedXML.Excel;
using UtilityData.Application.Abstractions;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Endpoints;

public static class ImportMappingConfigurationEndpoints
{
    private const long MaxSampleFileBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> CoreFields = new(StringComparer.OrdinalIgnoreCase)
    { "AccountNumber", "CustomerName", "Email", "ServiceAddress" };

    public static IEndpointRouteBuilder MapImportMappingConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/import-mappings").WithTags("Import Mapping Configuration").RequireAuthorization();
        group.MapGet("/{id:guid}/configuration", GetConfigurationAsync);
        group.MapPost("/preview", PreviewAsync)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"))
            .DisableAntiforgery();
        group.MapPost("/{id:guid}/versions", SaveVersionAsync)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));
        return app;
    }

    private static async Task<IResult> GetConfigurationAsync(Guid id, AppDbContext db, ICurrentTenant tenant, CancellationToken ct)
    {
        var mapping = await db.ImportMappings.AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenant.TenantId)
            .Select(x => new { x.Id, x.Name, x.Description, x.FileFormat, x.IsActive })
            .FirstOrDefaultAsync(ct);
        if (mapping is null) return Results.NotFound();

        var latest = await db.ImportMappingVersions.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.ImportMappingId == id)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(ct);
        if (latest is null) return Results.Problem("The mapping has no version. Recreate the mapping or contact an administrator.");

        var columns = await db.ImportMappingColumns.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.ImportMappingVersionId == latest.Id)
            .OrderBy(x => x.SourceColumnName)
            .Select(x => new MappingColumnDto(x.Id, x.SourceColumnName, x.TargetKind,
                x.TargetFieldKey, x.TenantCustomFieldId, x.IsRequired))
            .ToListAsync(ct);

        var customFields = await db.TenantCustomFields.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.IsActive)
            .OrderBy(x => x.Label)
            .Select(x => new CustomFieldOptionDto(x.Id, x.Name, x.Label, x.DataType, x.IsRequired))
            .ToListAsync(ct);

        return Results.Ok(new MappingConfigurationDto(
            mapping.Id, mapping.Name, mapping.Description, mapping.FileFormat, mapping.IsActive,
            latest.Id, latest.VersionNumber, latest.WorksheetName, latest.HeaderRowNumber,
            latest.DateFormat, latest.DecimalSeparator, latest.ThousandsSeparator,
            columns, customFields));
    }

    private static async Task<IResult> PreviewAsync(
        IFormFile? file,
        [FromForm] string? worksheetName,
        [FromForm] int headerRowNumber,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0) return Validation("file", "Select a non-empty CSV or XLSX file.");
        if (file.Length > MaxSampleFileBytes) return Validation("file", "Sample files cannot exceed 10 MB.");
        if (headerRowNumber < 1 || headerRowNumber > 1000) return Validation("headerRowNumber", "Header row must be between 1 and 1000.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        try
        {
            await using var stream = file.OpenReadStream();
            if (extension == ".csv")
            {
                var headers = await ReadCsvHeadersAsync(stream, headerRowNumber, ct);
                return Results.Ok(new FilePreviewDto(file.FileName, "Csv", null, headers));
            }
            if (extension == ".xlsx")
            {
                using var workbook = new XLWorkbook(stream);
                var sheets = workbook.Worksheets.Select(x => x.Name).ToList();
                if (sheets.Count == 0) return Validation("file", "The workbook does not contain any worksheets.");
                var selected = string.IsNullOrWhiteSpace(worksheetName) ? workbook.Worksheet(1) : workbook.Worksheets.FirstOrDefault(x => x.Name == worksheetName);
                if (selected is null) return Validation("worksheetName", "Select a worksheet that exists in the uploaded workbook.");
                var row = selected.Row(headerRowNumber);
                var last = selected.LastColumnUsed()?.ColumnNumber() ?? 0;
                var headers = Enumerable.Range(1, last)
                    .Select(i => row.Cell(i).GetString().Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (headers.Count == 0) return Validation("file", "No column headers were found in the selected worksheet and header row.");
                return Results.Ok(new FilePreviewDto(file.FileName, "Xlsx", selected.Name, headers, sheets));
            }
            return Validation("file", "Only .csv and .xlsx sample files are supported.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Validation("file", $"The sample file could not be read: {ex.Message}");
        }
    }

    private static async Task<List<string>> ReadCsvHeadersAsync(Stream stream, int headerRowNumber, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var parser = new TextFieldParser(reader) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
        parser.SetDelimiters(",", ";", "\t", "|");
        string[]? fields = null;
        for (var row = 1; row <= headerRowNumber && !parser.EndOfData; row++)
        {
            ct.ThrowIfCancellationRequested();
            fields = parser.ReadFields();
        }
        var headers = (fields ?? []).Select(x => x?.Trim() ?? "").ToList();
        if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace)) throw new InvalidDataException("No headers were found on the selected row.");
        if (headers.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count(x => !string.IsNullOrWhiteSpace(x)))
            throw new InvalidDataException("The header row contains duplicate column names. Rename duplicate columns in the source file.");
        return headers.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    private static async Task<IResult> SaveVersionAsync(
        Guid id, SaveMappingVersionRequest request, AppDbContext db, ICurrentTenant tenant, CancellationToken ct)
    {
        if (request.HeaderRowNumber is < 1 or > 1000) return Validation("headerRowNumber", "Header row must be between 1 and 1000.");
        if (request.Columns is null || request.Columns.Count == 0) return Validation("columns", "Preview the source file and configure at least one column.");
        if (request.Columns.Count > 250) return Validation("columns", "A mapping cannot contain more than 250 columns.");
        if (request.DateFormat?.Length > 80) return Validation("dateFormat", "Date format cannot exceed 80 characters.");
        if (request.DecimalSeparator is not ("." or ",")) return Validation("decimalSeparator", "Decimal separator must be a dot or comma.");
        if (request.ThousandsSeparator is not (null or "" or "." or "," or " ")) return Validation("thousandsSeparator", "Choose a supported thousands separator.");
        if (request.DecimalSeparator == request.ThousandsSeparator) return Validation("thousandsSeparator", "Decimal and thousands separators must be different.");

        var mapping = await db.ImportMappings.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId, ct);
        if (mapping is null) return Results.NotFound();
        if (!mapping.IsActive) return Results.Conflict(new { message = "Activate this mapping before editing its configuration." });

        var sourceNames = request.Columns.Select(x => x.SourceColumnName?.Trim() ?? "").ToList();
        if (sourceNames.Any(string.IsNullOrWhiteSpace) || sourceNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != sourceNames.Count)
            return Validation("columns", "Source column names must be present and unique.");

        var customIds = request.Columns.Where(x => x.TargetKind == ImportMappingTargetKind.TenantCustomField)
            .Select(x => x.TenantCustomFieldId ?? Guid.Empty).Distinct().ToList();
        var customFields = await db.TenantCustomFields
            .Where(x => x.TenantId == tenant.TenantId && x.IsActive && customIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        if (customFields.Count != customIds.Count) return Validation("columns", "One or more custom field destinations are unavailable or inactive for this tenant.");

        foreach (var column in request.Columns)
        {
            if (column.TargetKind == ImportMappingTargetKind.CoreField)
            {
                if (string.IsNullOrWhiteSpace(column.TargetFieldKey) || !CoreFields.Contains(column.TargetFieldKey))
                    return Validation("columns", "A core destination is invalid. Choose Account Number, Customer Name, Email or Service Address.");
                if (column.TenantCustomFieldId.HasValue) return Validation("columns", "A core destination cannot include a custom field ID.");
            }
            else if (column.TargetKind == ImportMappingTargetKind.TenantCustomField)
            {
                if (!column.TenantCustomFieldId.HasValue || !string.IsNullOrWhiteSpace(column.TargetFieldKey))
                    return Validation("columns", "A custom destination must reference exactly one active tenant custom field.");
            }
            else return Validation("columns", "A mapping destination type is invalid.");
        }

        var mappedCoreKeys = request.Columns
            .Where(x => x.TargetKind == ImportMappingTargetKind.CoreField)
            .Select(x => x.TargetFieldKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!mappedCoreKeys.Contains("AccountNumber") || !mappedCoreKeys.Contains("CustomerName"))
            return Validation("columns", "Account Number and Customer Name must both be mapped because they are required core customer fields.");

        var requiredCustomFields = await db.TenantCustomFields.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.IsActive && x.IsRequired)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var mappedCustomIds = request.Columns
            .Where(x => x.TargetKind == ImportMappingTargetKind.TenantCustomField && x.TenantCustomFieldId.HasValue)
            .Select(x => x.TenantCustomFieldId!.Value)
            .ToHashSet();
        if (requiredCustomFields.Any(x => !mappedCustomIds.Contains(x)))
            return Validation("columns", "Every active required tenant custom field must be mapped before this mapping can be saved.");

        var destinations = request.Columns.Select(x => x.TargetKind == ImportMappingTargetKind.CoreField
            ? $"core:{x.TargetFieldKey}" : $"custom:{x.TenantCustomFieldId}").ToList();
        if (destinations.Distinct(StringComparer.OrdinalIgnoreCase).Count() != destinations.Count)
            return Validation("columns", "Each destination field can be mapped only once.");

        var latestNumber = await db.ImportMappingVersions
            .Where(x => x.TenantId == tenant.TenantId && x.ImportMappingId == id)
            .Select(x => (int?)x.VersionNumber).MaxAsync(ct) ?? 0;
        var version = new ImportMappingVersion(tenant.TenantId, id, latestNumber + 1,
            request.WorksheetName, request.HeaderRowNumber, request.DateFormat,
            request.DecimalSeparator, string.IsNullOrEmpty(request.ThousandsSeparator) ? null : request.ThousandsSeparator);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.ImportMappingVersions.Add(version);
        foreach (var column in request.Columns)
        {
            var required = column.IsRequired;
            if (column.TargetKind == ImportMappingTargetKind.CoreField &&
                (column.TargetFieldKey!.Equals("AccountNumber", StringComparison.OrdinalIgnoreCase) ||
                 column.TargetFieldKey.Equals("CustomerName", StringComparison.OrdinalIgnoreCase)))
                required = true;
            if (column.TargetKind == ImportMappingTargetKind.TenantCustomField && customFields[column.TenantCustomFieldId!.Value].IsRequired)
                required = true;
            db.ImportMappingColumns.Add(new ImportMappingColumn(tenant.TenantId, version.Id,
                column.SourceColumnName.Trim(), column.TargetKind,
                column.TargetKind == ImportMappingTargetKind.CoreField ? column.TargetFieldKey!.Trim() : null,
                column.TargetKind == ImportMappingTargetKind.TenantCustomField ? column.TenantCustomFieldId : null,
                required));
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Results.Created($"/api/import-mappings/{id}/configuration", new { versionId = version.Id, version.VersionNumber, message = $"Mapping version {version.VersionNumber} was saved." });
    }

    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    public sealed record MappingConfigurationDto(Guid Id, string Name, string? Description, ImportFileFormat FileFormat,
        bool IsActive, Guid CurrentVersionId, int CurrentVersionNumber, string? WorksheetName, int HeaderRowNumber,
        string? DateFormat, string? DecimalSeparator, string? ThousandsSeparator,
        IReadOnlyList<MappingColumnDto> Columns, IReadOnlyList<CustomFieldOptionDto> CustomFields);
    public sealed record MappingColumnDto(Guid Id, string SourceColumnName, ImportMappingTargetKind TargetKind,
        string? TargetFieldKey, Guid? TenantCustomFieldId, bool IsRequired);
    public sealed record CustomFieldOptionDto(Guid Id, string Name, string Label, CustomFieldDataType DataType, bool IsRequired);
    public sealed record FilePreviewDto(string FileName, string FileFormat, string? WorksheetName,
        IReadOnlyList<string> Headers, IReadOnlyList<string>? Worksheets = null);
    public sealed record SaveMappingVersionRequest(string? WorksheetName, int HeaderRowNumber, string? DateFormat,
        string DecimalSeparator, string? ThousandsSeparator, IReadOnlyList<SaveMappingColumnRequest> Columns);
    public sealed record SaveMappingColumnRequest(string SourceColumnName, ImportMappingTargetKind TargetKind,
        string? TargetFieldKey, Guid? TenantCustomFieldId, bool IsRequired);
}
