using Microsoft.AspNetCore.Mvc;
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
        var selected = await (from version in db.ImportMappingVersions
            join mapping in db.ImportMappings on version.ImportMappingId equals mapping.Id
            where version.Id == mappingVersionId && version.TenantId == tenant.TenantId
                && mapping.TenantId == tenant.TenantId && mapping.IsActive && mapping.FileFormat == format.Value
            select new { version.Id }).FirstOrDefaultAsync(ct);
        if (selected is null) return Results.BadRequest(new { message = "Select an active mapping version for this tenant that matches the uploaded file format." });

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
