using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UtilityData.Application.Abstractions;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenant")
            .WithTags("Tenant")
            .RequireAuthorization();

        group.MapGet("/me", async (
            ICurrentTenant currentTenant,
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == currentTenant.TenantId,
                    ct);

            if (tenant is null)
                return Results.NotFound();

            return Results.Ok(new
            {
                TenantId = tenant.Id,
                UserId = currentTenant.UserId,
                currentTenant.Role,
                Email = user.FindFirstValue(ClaimTypes.Email)
                    ?? user.FindFirstValue("email"),
                TenantName = tenant.Name,
                TenantCode = tenant.Code,
                tenant.IsActive
            });
        });

        group.MapGet(
            "/admin-check",
            [Authorize(Roles = "Admin")] () =>
                Results.Ok(new
                {
                    authorized = true,
                    requiredRole = "Admin"
                }));

        return app;
    }
}