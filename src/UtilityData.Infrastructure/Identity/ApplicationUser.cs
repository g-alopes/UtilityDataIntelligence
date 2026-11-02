using Microsoft.AspNetCore.Identity;
namespace UtilityData.Infrastructure.Identity;
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
