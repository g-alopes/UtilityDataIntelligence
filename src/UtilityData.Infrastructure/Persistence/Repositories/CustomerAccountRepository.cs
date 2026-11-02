using Microsoft.EntityFrameworkCore;
using UtilityData.Application.Abstractions;
using UtilityData.Domain.Entities;

namespace UtilityData.Infrastructure.Persistence.Repositories;

internal sealed class CustomerAccountRepository(AppDbContext dbContext) : ICustomerAccountRepository
{
    public async Task<IReadOnlyList<CustomerAccount>> GetRecentAsync(
        Guid tenantId, int take, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (take is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(take), "Take must be between 1 and 500.");

        return await dbContext.CustomerAccounts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        return dbContext.CustomerAccounts
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<CustomerAccount> accounts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        var items = accounts.ToArray();
        if (items.Length == 0) return;

        var tenantIds = items.Select(x => x.TenantId).Distinct().ToArray();
        if (tenantIds.Length != 1 || tenantIds[0] == Guid.Empty)
            throw new InvalidOperationException("A batch must contain accounts from exactly one valid tenant.");

        await dbContext.CustomerAccounts.AddRangeAsync(items, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
