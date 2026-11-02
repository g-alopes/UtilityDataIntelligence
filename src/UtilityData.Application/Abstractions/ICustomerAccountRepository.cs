using UtilityData.Domain.Entities;

namespace UtilityData.Application.Abstractions;

public interface ICustomerAccountRepository
{
    Task<IReadOnlyList<CustomerAccount>> GetRecentAsync(
        Guid tenantId, int take, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<CustomerAccount> accounts, CancellationToken cancellationToken = default);
}
