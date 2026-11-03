using Microsoft.EntityFrameworkCore;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Endpoints;

public static class ReadEndpoints
{
    public static IEndpointRouteBuilder MapReadEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        api.MapGet("/dashboard/summary", async (AppDbContext db, HttpContext context) =>
        {
            var tenantId = ReadTenantId(context);
            if (tenantId is null) return Results.Unauthorized();

            var batches = db.ImportBatches.Where(x => x.TenantId == tenantId.Value);
            var accountCount = await db.CustomerAccounts.CountAsync(x => x.TenantId == tenantId.Value);
            var batchCount = await batches.CountAsync();
            var pending = await batches.CountAsync(x => x.Status == ImportBatchStatus.Completed);
            var invalid = await db.ImportRecords
                .Where(r => batches.Select(b => b.Id).Contains(r.ImportBatchId) && !r.IsValid)
                .CountAsync();

            return Results.Ok(new { CustomerAccountCount=accountCount, ImportBatchCount=batchCount,
                PendingApprovalCount=pending, InvalidRecordCount=invalid });
        });

        api.MapGet("/customer-accounts", async (int? skip, int? take, AppDbContext db, HttpContext context) =>
        {
            var tenantId = ReadTenantId(context);
            if (tenantId is null) return Results.Unauthorized();
            var offset = Math.Max(skip ?? 0, 0);
            var size = Math.Clamp(take ?? 25, 1, 100);
            var query = db.CustomerAccounts.AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value).OrderBy(x => x.AccountNumber);
            var total = await query.CountAsync();
            var items = await query.Skip(offset).Take(size).Select(x => new {
                x.Id, x.AccountNumber, x.CustomerName, x.Email, x.CreatedAtUtc
            }).ToListAsync();
            return Results.Ok(new { Items=items, TotalCount=total, Skip=offset, Take=size });
        });

        return app;
    }

    private static Guid? ReadTenantId(HttpContext context)
    {
        var raw = context.User.FindFirst("tenant_id")?.Value
            ?? context.User.FindFirst("TenantId")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
