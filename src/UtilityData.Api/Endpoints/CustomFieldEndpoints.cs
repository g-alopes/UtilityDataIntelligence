using Microsoft.EntityFrameworkCore;
using UtilityData.Application.Abstractions;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Endpoints;

public static class CustomFieldEndpoints
{
    public static IEndpointRouteBuilder MapCustomFieldEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/custom-fields")
            .WithTags("Tenant Custom Fields")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync).RequireAuthorization(policy => policy.RequireRole("Admin"));
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(policy => policy.RequireRole("Admin"));
        group.MapPost("/{id:guid}/activate", ActivateAsync).RequireAuthorization(policy => policy.RequireRole("Admin"));
        group.MapPost("/{id:guid}/deactivate", DeactivateAsync).RequireAuthorization(policy => policy.RequireRole("Admin"));

        return app;
    }

    private static async Task<IResult> ListAsync(
        AppDbContext db, ICurrentTenant tenant, CancellationToken ct)
    {
        var items = await db.TenantCustomFields.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId)
            .OrderBy(x => x.Name)
            .Select(x => new CustomFieldResponse(
                x.Id, x.Name, x.Label, x.DataType, x.IsRequired,
                x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> GetAsync(
        Guid id, AppDbContext db, ICurrentTenant tenant, CancellationToken ct)
    {
        var item = await db.TenantCustomFields.AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenant.TenantId)
            .Select(x => new CustomFieldResponse(
                x.Id, x.Name, x.Label, x.DataType, x.IsRequired,
                x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc))
            .FirstOrDefaultAsync(ct);

        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateAsync(
        CreateCustomFieldRequest request,
        AppDbContext db,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 80)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Name)] = ["Name is required and cannot exceed 80 characters."]
            });

        if (string.IsNullOrWhiteSpace(request.Label) || request.Label.Length > 160)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Label)] = ["Label is required and cannot exceed 160 characters."]
            });

        if (!Enum.IsDefined(request.DataType))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.DataType)] = ["Select a valid data type."]
            });

        var normalizedName = request.Name.Trim();
        var exists = await db.TenantCustomFields.AnyAsync(
            x => x.TenantId == tenant.TenantId && x.Name == normalizedName, ct);
        if (exists)
            return Results.Conflict(new { message = "A custom field with this name already exists in this tenant." });

        TenantCustomField field;
        try
        {
            field = new TenantCustomField(
                tenant.TenantId, normalizedName, request.Label,
                request.DataType, request.IsRequired);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Name)] = [ex.Message]
            });
        }

        db.TenantCustomFields.Add(field);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { message = "A custom field with this name already exists in this tenant." });
        }

        return Results.Created($"/api/custom-fields/{field.Id}", ToResponse(field));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateCustomFieldRequest request,
        AppDbContext db,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Label) || request.Label.Length > 160)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Label)] = ["Label is required and cannot exceed 160 characters."]
            });

        var field = await db.TenantCustomFields.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == tenant.TenantId, ct);
        if (field is null) return Results.NotFound();

        field.Update(request.Label, request.IsRequired);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(field));
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
        var field = await db.TenantCustomFields.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == tenant.TenantId, ct);
        if (field is null) return Results.NotFound();

        if (active) field.Activate(); else field.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(field));
    }

    private static CustomFieldResponse ToResponse(TenantCustomField x) => new(
        x.Id, x.Name, x.Label, x.DataType, x.IsRequired,
        x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);

    public sealed record CreateCustomFieldRequest(
        string Name, string Label, CustomFieldDataType DataType, bool IsRequired);

    public sealed record UpdateCustomFieldRequest(string Label, bool IsRequired);

    public sealed record CustomFieldResponse(
        Guid Id, string Name, string Label, CustomFieldDataType DataType,
        bool IsRequired, bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
}
