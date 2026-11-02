using UtilityData.Domain.Entities;

namespace UtilityData.Application.Abstractions;

public interface ICustomerAccountRepository
{
    Task<IReadOnlyList<CustomerAccount>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<CustomerAccount> accounts, CancellationToken cancellationToken = default);
}
