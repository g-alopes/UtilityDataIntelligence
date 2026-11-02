namespace UtilityData.Domain.Entities;

public sealed class CustomerAccount
{
    private CustomerAccount() { }

    public CustomerAccount(
        Guid tenantId,
        string accountNumber,
        string customerName,
        string? email,
        string? serviceAddress)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(accountNumber)) throw new ArgumentException("Account number is required.", nameof(accountNumber));
        if (string.IsNullOrWhiteSpace(customerName)) throw new ArgumentException("Customer name is required.", nameof(customerName));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        AccountNumber = accountNumber.Trim();
        CustomerName = customerName.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        ServiceAddress = string.IsNullOrWhiteSpace(serviceAddress) ? null : serviceAddress.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? ServiceAddress { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
