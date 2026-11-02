using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UtilityData.Api.Authentication;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Identity;
using UtilityData.Infrastructure.Persistence;
namespace UtilityData.Api.Endpoints;
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");
        group.MapPost("/register", RegisterAsync).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        return app;
    }
    private static async Task<IResult> RegisterAsync(RegisterRequest request, UserManager<ApplicationUser> users,
        AppDbContext db, ITokenService tokens, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TenantName) || string.IsNullOrWhiteSpace(request.TenantCode) ||
            string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { message = "Tenant name, tenant code, email and password are required." });
        if (request.Password.Length < 10)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["password"] = ["Password must contain at least 10 characters."] });
        var code = request.TenantCode.Trim().ToUpperInvariant();
        if (await db.Tenants.AnyAsync(x => x.Code == code, ct))
            return Results.Conflict(new { message = "Tenant code is already registered." });
        var email = request.Email.Trim();
        if (await users.FindByEmailAsync(email) is not null)
            return Results.Conflict(new { message = "Email is already registered." });
        var tenant = new Tenant(request.TenantName, code);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email };
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            await tx.RollbackAsync(ct);
            return Results.ValidationProblem(created.Errors.GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }
        var roleResult = await users.AddToRoleAsync(user, "Admin");
        if (!roleResult.Succeeded)
        {
            await tx.RollbackAsync(ct);
            return Results.Problem("Could not assign initial tenant administrator role.", statusCode: 500);
        }
        db.TenantMemberships.Add(new TenantMembership(user.Id, tenant.Id, "Admin"));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        var token = tokens.CreateAccessToken(user, tenant.Id, "Admin");
        return Results.Created("/api/tenant/me", new AuthResponse(token, "Bearer",
            DateTime.UtcNow.AddMinutes(30), user.Id, tenant.Id, "Admin"));
    }
    private static async Task<IResult> LoginAsync(LoginRequest request, UserManager<ApplicationUser> users,
        AppDbContext db, ITokenService tokens, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email?.Trim() ?? "");
        if (user is null || !await users.CheckPasswordAsync(user, request.Password ?? ""))
            return Results.Unauthorized();
        var membership = await db.TenantMemberships.AsNoTracking()
            .Where(x => x.UserId == user.Id).OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (membership is null) return Results.Problem("User has no tenant membership.", statusCode: 403);
        var active = await db.Tenants.AnyAsync(x => x.Id == membership.TenantId && x.IsActive, ct);
        if (!active) return Results.Problem("Tenant is inactive.", statusCode: 403);
        var token = tokens.CreateAccessToken(user, membership.TenantId, membership.Role);
        return Results.Ok(new AuthResponse(token, "Bearer", DateTime.UtcNow.AddMinutes(30),
            user.Id, membership.TenantId, membership.Role));
    }
    public sealed record RegisterRequest(string TenantName, string TenantCode, string Email, string Password);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record AuthResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc,
        Guid UserId, Guid TenantId, string Role);
}
