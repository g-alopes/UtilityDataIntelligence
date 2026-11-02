using Microsoft.EntityFrameworkCore;
using UtilityData.Application.Abstractions;
using UtilityData.Domain.Entities;

namespace UtilityData.Infrastructure.Persistence.Repositories;

internal sealed class CustomerAccountRepository(AppDbContext db, ICurrentTenant tenant) : ICustomerAccountRepository
{
    public async Task<IReadOnlyList<CustomerAccount>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        if (take is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(take));
        return await db.CustomerAccounts.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId)
            .OrderByDescending(x => x.CreatedAtUtc).Take(take).ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        db.CustomerAccounts.CountAsync(x => x.TenantId == tenant.TenantId, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<CustomerAccount> accounts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        var rows = accounts.ToArray();
        if (rows.Length == 0) return;
        if (rows.Any(x => x.TenantId != tenant.TenantId))
            throw new UnauthorizedAccessException("One or more accounts do not belong to the current tenant.");
        await db.CustomerAccounts.AddRangeAsync(rows, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
