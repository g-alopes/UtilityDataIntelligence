using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using Microsoft.VisualBasic.FileIO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UtilityData.Api.Imports;
using UtilityData.Application.Abstractions;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Endpoints;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/imports").WithTags("Imports").RequireAuthorization();

        group.MapPost("/", UploadAsync).DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(52 * 1024 * 1024));
        group.MapGet("/{batchId:guid}", StatusAsync);
        group.MapGet("/{batchId:guid}/records", RecordsAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/mapping-options", MappingOptionsAsync);
        group.MapPost("/{batchId:guid}/approve", ApproveAsync)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));

        return app;
    }


    private static async Task<IResult> MappingOptionsAsync(AppDbContext db, ICurrentTenant tenant, CancellationToken ct)
    {
        var mappings = await db.ImportMappings.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.IsActive)
            .Select(x => new
            {
                x.Id, x.Name, x.FileFormat,
                Version = db.ImportMappingVersions.AsNoTracking()
                    .Where(v => v.TenantId == tenant.TenantId && v.ImportMappingId == x.Id)
                    .OrderByDescending(v => v.VersionNumber)
                    .Select(v => new { v.Id, v.VersionNumber, v.WorksheetName })
                    .FirstOrDefault()
            }).Where(x => x.Version != null).OrderBy(x => x.Name).ToListAsync(ct);
        return Results.Ok(mappings.Select(x => new
        {
            MappingId = x.Id, x.Name, FileFormat = x.FileFormat.ToString(),
            MappingVersionId = x.Version!.Id, x.Version.VersionNumber, x.Version.WorksheetName
        }));
    }

    private static async Task<IResult> UploadAsync(
        IFormFile? file,
        [FromForm] Guid mappingVersionId,
        AppDbContext db,
        ICurrentTenant tenant,
        IOptions<ImportStorageOptions> settings,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "Choose a non-empty CSV or XLSX file." });
        if (file.Length > settings.Value.MaxFileBytes)
            return Results.BadRequest(new { message = "File exceeds configured size limit." });
        var extension = Path.GetExtension(file.FileName);
        var format = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) ? ImportFileFormat.Csv
            : extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ? ImportFileFormat.Xlsx : (ImportFileFormat?)null;
        if (format is null) return Results.BadRequest(new { message = "Only CSV and XLSX files are accepted." });
        var selected = await (from version in db.ImportMappingVersions.AsNoTracking()
            join mapping in db.ImportMappings.AsNoTracking() on version.ImportMappingId equals mapping.Id
            where version.Id == mappingVersionId && version.TenantId == tenant.TenantId
                && mapping.TenantId == tenant.TenantId && mapping.IsActive && mapping.FileFormat == format.Value
            select new { Version = version }).FirstOrDefaultAsync(ct);
        if (selected is null) return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Import mapping is not valid",
            detail: "Select an active mapping version for this tenant that matches the uploaded file format.");

        // Validate the file contract synchronously, before writing it to storage or creating a batch.
        var configuredHeaders = await db.ImportMappingColumns.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.ImportMappingVersionId == selected.Version.Id)
            .Select(x => x.SourceColumnName)
            .ToListAsync(ct);
        if (configuredHeaders.Count == 0)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid import mapping",
                detail: "The selected mapping version has no configured source columns.");

        string? headerValidationError;
        try
        {
            await using var validationStream = file.OpenReadStream();
            headerValidationError = ValidateHeaders(validationStream, format.Value,
                selected.Version.WorksheetName, selected.Version.HeaderRowNumber, configuredHeaders, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Unable to validate uploaded file",
                detail: $"The uploaded file could not be read: {ex.Message}");
        }
        if (headerValidationError is not null)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Uploaded file does not match the selected mapping",
                detail: headerValidationError);

        var batchId = Guid.NewGuid();
        var root = Path.IsPathRooted(settings.Value.RootPath)
            ? settings.Value.RootPath
            : Path.Combine(env.ContentRootPath, settings.Value.RootPath);
        var folder = Path.Combine(root, tenant.TenantId.ToString("N"));
        Directory.CreateDirectory(folder);
        var storedPath = Path.Combine(folder, $"{batchId:N}{(format == ImportFileFormat.Xlsx ? ".xlsx" : ".csv")}");

        try
        {
            await using (var output = new FileStream(
                storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await file.CopyToAsync(output, ct);
            }

            var batch = new ImportBatch(tenant.TenantId, tenant.UserId, file.FileName, storedPath, mappingVersionId);
            db.ImportBatches.Add(batch);
            await db.SaveChangesAsync(ct);

            return Results.Accepted($"/api/imports/{batch.Id}",
                new { batchId = batch.Id, status = "Pending" });
        }
        catch
        {
            if (File.Exists(storedPath))
                File.Delete(storedPath);
            throw;
        }
    }

    private static string? ValidateHeaders(
        Stream stream,
        ImportFileFormat format,
        string? worksheetName,
        int headerRowNumber,
        IReadOnlyCollection<string> configuredHeaders,
        CancellationToken ct)
    {
        string[] headers;
        if (format == ImportFileFormat.Csv)
        {
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            using var parser = new TextFieldParser(reader)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };
            parser.SetDelimiters(",", ";", "\t", "|");
            string[]? fields = null;
            for (var row = 1; row <= headerRowNumber; row++)
            {
                ct.ThrowIfCancellationRequested();
                if (parser.EndOfData)
                    return $"The configured header row ({headerRowNumber}) is beyond the end of the CSV file.";
                fields = parser.ReadFields();
            }
            headers = fields ?? [];
        }
        else
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = string.IsNullOrWhiteSpace(worksheetName)
                ? workbook.Worksheets.FirstOrDefault()
                : workbook.Worksheets.FirstOrDefault(x => string.Equals(x.Name, worksheetName, StringComparison.Ordinal));
            if (worksheet is null)
                return string.IsNullOrWhiteSpace(worksheetName)
                    ? "The uploaded workbook does not contain a worksheet."
                    : $"The configured worksheet '{worksheetName}' was not found in the uploaded workbook.";
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            if (headerRowNumber > lastRow || lastColumn == 0)
                return $"The configured header row ({headerRowNumber}) is empty or outside the selected worksheet.";
            headers = Enumerable.Range(1, lastColumn)
                .Select(column => worksheet.Cell(headerRowNumber, column).GetString())
                .ToArray();
        }

        var headerIndexes = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = headers.Where(x => !string.IsNullOrEmpty(x))
            .GroupBy(x => x, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length > 0)
            return "The uploaded file contains duplicate column header(s): " +
                string.Join(", ", duplicates.Select(x => $"'{x}'")) + ". Column headers must be unique.";

        foreach (var header in headers)
            if (!string.IsNullOrEmpty(header)) headerIndexes.Add(header);

        var missing = configuredHeaders
            .Where(header => !headerIndexes.Contains(header))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        if (missing.Length == 0) return null;

        return "The uploaded file is missing mapped column header(s): " +
            string.Join(", ", missing.Select(x => $"'{x}'")) +
            ". Header names must match exactly, including capitalization, spaces and punctuation. No import batch was created.";
    }

    private static async Task<IResult> StatusAsync(
        Guid batchId, AppDbContext db, ICurrentTenant tenant, CancellationToken ct)
    {
        var result = await db.ImportBatches.AsNoTracking()
            .Where(x => x.Id == batchId && x.TenantId == tenant.TenantId)
            .Select(x => new
            {
                x.Id,
                x.FileName,
                x.ImportMappingVersionId,
                x.Status,
                x.TotalRecords,
                x.ValidRecords,
                x.InvalidRecords,
                x.ApprovedRecordCount,
                x.ApprovedByUserId,
                x.ApprovedAtUtc,
                x.ErrorMessage,
                x.CreatedAtUtc,
                x.CompletedAtUtc
            })
            .FirstOrDefaultAsync(ct);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> RecordsAsync(
        Guid batchId,
        AppDbContext db,
        ICurrentTenant tenant,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default)
    {
        if (skip < 0 || take is < 1 or > 500)
            return Results.BadRequest(new { message = "skip must be >= 0 and take must be between 1 and 500." });

        var exists = await db.ImportBatches.AnyAsync(
            x => x.Id == batchId && x.TenantId == tenant.TenantId, ct);
        if (!exists)
            return Results.NotFound();

        var rows = await db.ImportRecords.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.ImportBatchId == batchId)
            .OrderBy(x => x.RowNumber)
            .Skip(skip)
            .Take(take)
            .Select(x => new
            {
                x.RowNumber,
                x.AccountNumber,
                x.CustomerName,
                x.Email,
                x.ServiceAddress,
                x.IsValid,
                x.Errors
            })
            .ToListAsync(ct);

        return Results.Ok(new { batchId, skip, take, records = rows });
    }

    private static async Task<IResult> ListAsync(
    AppDbContext db,
    ICurrentTenant tenant,
    int skip = 0,
    int take = 25,
    CancellationToken ct = default)
    {
        if (skip < 0 || take is < 1 or > 100)
            return Results.BadRequest(new
            {
                message = "skip must be >= 0 and take must be between 1 and 100."
            });

        var query = db.ImportBatches.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId);

        var totalCount = await query.CountAsync(ct);

        var batches = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        var items = batches.Select(x => new
        {
            x.Id,
            x.FileName,
            x.ImportMappingVersionId,
            Status = x.Status.ToString(),
            TotalRecordCount = x.TotalRecords,
            ValidRecordCount = x.ValidRecords,
            InvalidRecordCount = x.InvalidRecords,
            x.ApprovedRecordCount,
            x.CreatedAtUtc
        });

        return Results.Ok(new
        {
            items,
            totalCount,
            skip,
            take
        });
    }

    private static async Task<IResult> ApproveAsync(
    Guid batchId,
    ICurrentTenant tenant,
    ImportApprovalService approvalService,
    CancellationToken ct)
    {
        try
        {
            var imported = await approvalService.ApproveAsync(
                batchId, tenant.TenantId, tenant.UserId, ct);

            return Results.Ok(new
            {
                batchId,
                status = ImportBatchStatus.Approved.ToString(),
                importedRecordCount = imported,
                message = "Valid records were committed to CustomerAccounts."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Import batch not found",
                detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Import approval rejected",
                detail: ex.Message);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Import approval conflict",
                detail: "One or more account numbers now exist for this tenant. The approval was rolled back; review the data and re-import.");
        }
    }
}
