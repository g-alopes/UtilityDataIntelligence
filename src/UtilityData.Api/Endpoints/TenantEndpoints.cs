using Microsoft.AspNetCore.Authorization;
using UtilityData.Application.Abstractions;
namespace UtilityData.Api.Endpoints;
public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenant").WithTags("Tenant").RequireAuthorization();
        group.MapGet("/me", (ICurrentTenant tenant) => Results.Ok(new
        {
            tenant.TenantId, tenant.UserId, tenant.Role
        }));
        group.MapGet("/admin-check", [Authorize(Roles = "Admin")] () =>
            Results.Ok(new { authorized = true, requiredRole = "Admin" }));
        return app;
    }
}
