using Microsoft.EntityFrameworkCore;
using UtilityData.Application.Abstractions;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Endpoints;

public static class ImportMappingEndpoints
{
    private const int MaxNameLength = 160;
    private const int MaxDescriptionLength = 1000;

    public static IEndpointRouteBuilder MapImportMappingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/import-mappings")
            .WithTags("Import Mappings")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync).RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));
        group.MapPost("/{id:guid}/activate", ActivateAsync).RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));
        group.MapPost("/{id:guid}/deactivate", DeactivateAsync).RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));

        return app;
    }

    private static async Task<IResult> ListAsync(
        AppDbContext db, ICurrentTenant tenant, CancellationToken ct)
    {
        var items = await db.ImportMappings.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId)
            .OrderBy(x => x.Name)
            .Select(x => new ImportMappingListItem(
                x.Id,
                x.Name,
                x.Description,
                x.FileFormat,
                x.IsActive,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                db.ImportMappingVersions
                    .Where(v => v.TenantId == tenant.TenantId && v.ImportMappingId == x.Id)
                    .Select(v => (int?)v.VersionNumber)
                    .Max() ?? 0,
                db.ImportMappingVersions.Count(v =>
                    v.TenantId == tenant.TenantId && v.ImportMappingId == x.Id)))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> GetAsync(
        Guid id, AppDbContext db, ICurrentTenant tenant, CancellationToken ct)
    {
        var mapping = await db.ImportMappings.AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenant.TenantId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.FileFormat,
                x.IsActive,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(ct);

        if (mapping is null) return Results.NotFound();

        var versions = await db.ImportMappingVersions.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.ImportMappingId == id)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new ImportMappingVersionSummary(
                x.Id,
                x.VersionNumber,
                x.WorksheetName,
                x.HeaderRowNumber,
                x.DateFormat,
                x.DecimalSeparator,
                x.ThousandsSeparator,
                x.CreatedAtUtc,
                db.ImportMappingColumns.Count(c =>
                    c.TenantId == tenant.TenantId && c.ImportMappingVersionId == x.Id)))
            .ToListAsync(ct);

        return Results.Ok(new ImportMappingDetailResponse(
            mapping.Id, mapping.Name, mapping.Description, mapping.FileFormat,
            mapping.IsActive, mapping.CreatedAtUtc, mapping.UpdatedAtUtc, versions));
    }

    private static async Task<IResult> CreateAsync(
        CreateImportMappingRequest request,
        AppDbContext db,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        var validation = Validate(request.Name, request.Description, request.FileFormat);
        if (validation is not null) return validation;

        var name = request.Name.Trim();
        var exists = await db.ImportMappings.AnyAsync(
            x => x.TenantId == tenant.TenantId && x.Name == name, ct);
        if (exists)
            return Results.Conflict(new { message = "An import mapping with this name already exists in this tenant." });

        var mapping = new ImportMapping(
            tenant.TenantId, name, request.Description, request.FileFormat);

        // Every mapping starts with an immutable version 1. Column definitions
        // are added in Phase 6.4; edits to a definition create later versions.
        var version = new ImportMappingVersion(
            tenant.TenantId,
            mapping.Id,
            versionNumber: 1,
            worksheetName: null,
            headerRowNumber: 1,
            dateFormat: null,
            decimalSeparator: ".",
            thousandsSeparator: ",");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.ImportMappings.Add(mapping);
        db.ImportMappingVersions.Add(version);

        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            return Results.Conflict(new { message = "An import mapping with this name already exists in this tenant." });
        }

        return Results.Created($"/api/import-mappings/{mapping.Id}",
            new ImportMappingListItem(
                mapping.Id, mapping.Name, mapping.Description, mapping.FileFormat,
                mapping.IsActive, mapping.CreatedAtUtc, mapping.UpdatedAtUtc, 1, 1));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateImportMappingRequest request,
        AppDbContext db,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > MaxNameLength)
            return Validation(nameof(request.Name), $"Name is required and cannot exceed {MaxNameLength} characters.");

        if (request.Description?.Length > MaxDescriptionLength)
            return Validation(nameof(request.Description), $"Description cannot exceed {MaxDescriptionLength} characters.");

        var mapping = await db.ImportMappings.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == tenant.TenantId, ct);
        if (mapping is null) return Results.NotFound();

        var name = request.Name.Trim();
        var duplicate = await db.ImportMappings.AnyAsync(
            x => x.TenantId == tenant.TenantId && x.Id != id && x.Name == name, ct);
        if (duplicate)
            return Results.Conflict(new { message = "An import mapping with this name already exists in this tenant." });

        mapping.UpdateDetails(name, request.Description);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { message = "An import mapping with this name already exists in this tenant." });
        }

        var versionCount = await db.ImportMappingVersions.CountAsync(
            x => x.TenantId == tenant.TenantId && x.ImportMappingId == id, ct);
        var latestVersion = await db.ImportMappingVersions
            .Where(x => x.TenantId == tenant.TenantId && x.ImportMappingId == id)
            .Select(x => (int?)x.VersionNumber)
            .MaxAsync(ct) ?? 0;

        return Results.Ok(new ImportMappingListItem(
            mapping.Id, mapping.Name, mapping.Description, mapping.FileFormat,
            mapping.IsActive, mapping.CreatedAtUtc, mapping.UpdatedAtUtc,
            latestVersion, versionCount));
    }

    private static Task<IResult> ActivateAsync(
        Guid id, AppDbContext db, ICurrentTenant tenant, CancellationToken ct) =>
        SetActiveAsync(id, true, db, tenant, ct);

    private static Task<IResult> DeactivateAsync(
        Guid id, AppDbContext db, ICurrentTenant tenant, CancellationToken ct) =>
        SetActiveAsync(id, false, db, tenant, ct);

    private static async Task<IResult> SetActiveAsync(
        Guid id, bool active, AppDbContext db, ICurrentTenant tenant, CancellationToken ct)
    {
        var mapping = await db.ImportMappings.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == tenant.TenantId, ct);
        if (mapping is null) return Results.NotFound();

        if (active) mapping.Activate(); else mapping.Deactivate();
        await db.SaveChangesAsync(ct);

        var latestVersion = await db.ImportMappingVersions
            .Where(x => x.TenantId == tenant.TenantId && x.ImportMappingId == id)
            .Select(x => (int?)x.VersionNumber)
            .MaxAsync(ct) ?? 0;
        var versionCount = await db.ImportMappingVersions.CountAsync(
            x => x.TenantId == tenant.TenantId && x.ImportMappingId == id, ct);

        return Results.Ok(new ImportMappingListItem(
            mapping.Id, mapping.Name, mapping.Description, mapping.FileFormat,
            mapping.IsActive, mapping.CreatedAtUtc, mapping.UpdatedAtUtc,
            latestVersion, versionCount));
    }

    private static IResult? Validate(string name, string? description, ImportFileFormat fileFormat)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
            return Validation(nameof(name), $"Name is required and cannot exceed {MaxNameLength} characters.");
        if (description?.Length > MaxDescriptionLength)
            return Validation(nameof(description), $"Description cannot exceed {MaxDescriptionLength} characters.");
        if (!Enum.IsDefined(fileFormat))
            return Validation(nameof(fileFormat), "Select CSV or XLSX as the file format.");
        return null;
    }

    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    public sealed record CreateImportMappingRequest(
        string Name, string? Description, ImportFileFormat FileFormat);

    public sealed record UpdateImportMappingRequest(string Name, string? Description);

    public sealed record ImportMappingListItem(
        Guid Id, string Name, string? Description, ImportFileFormat FileFormat,
        bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc,
        int LatestVersionNumber, int VersionCount);

    public sealed record ImportMappingDetailResponse(
        Guid Id, string Name, string? Description, ImportFileFormat FileFormat,
        bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc,
        IReadOnlyList<ImportMappingVersionSummary> Versions);

    public sealed record ImportMappingVersionSummary(
        Guid Id, int VersionNumber, string? WorksheetName, int HeaderRowNumber,
        string? DateFormat, string? DecimalSeparator, string? ThousandsSeparator,
        DateTime CreatedAtUtc, int ColumnCount);
}
