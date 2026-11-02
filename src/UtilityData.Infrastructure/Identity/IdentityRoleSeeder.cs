using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
namespace UtilityData.Infrastructure.Identity;
public static class IdentityRoleSeeder
{
    private static readonly string[] Roles = ["Admin", "Manager", "Analyst"];
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles)
        {
            if (await manager.RoleExistsAsync(role)) continue;
            var result = await manager.CreateAsync(new IdentityRole<Guid>(role));
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not seed role '{role}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }
}
