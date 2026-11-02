namespace UtilityData.Domain.Entities;

public sealed class ImportRecord
{
    private ImportRecord() { }
    public ImportRecord(Guid tenantId, Guid batchId, long rowNumber, string? accountNumber,
        string? customerName, string? email, string? serviceAddress, bool isValid, string? errors)
    {
        Id = Guid.NewGuid(); TenantId = tenantId; ImportBatchId = batchId; RowNumber = rowNumber;
        AccountNumber = accountNumber; CustomerName = customerName; Email = email;
        ServiceAddress = serviceAddress; IsValid = isValid; Errors = errors; CreatedAtUtc = DateTime.UtcNow;
    }
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ImportBatchId { get; private set; }
    public long RowNumber { get; private set; }
    public string? AccountNumber { get; private set; }
    public string? CustomerName { get; private set; }
    public string? Email { get; private set; }
    public string? ServiceAddress { get; private set; }
    public bool IsValid { get; private set; }
    public string? Errors { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
