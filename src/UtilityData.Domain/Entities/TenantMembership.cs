namespace UtilityData.Domain.Entities;

public sealed class TenantMembership
{
    private TenantMembership() { }
    public TenantMembership(Guid userId, Guid tenantId, string role)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (role is not ("Admin" or "Manager" or "Analyst"))
            throw new ArgumentException("Unsupported tenant role.", nameof(role));
        UserId = userId;
        TenantId = tenantId;
        Role = role;
        CreatedAtUtc = DateTime.UtcNow;
    }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Role { get; private set; } = "Analyst";
    public DateTime CreatedAtUtc { get; private set; }
}
