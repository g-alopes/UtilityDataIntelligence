namespace UtilityData.Domain.Entities;

public enum ImportBatchStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public sealed class ImportBatch
{
    private ImportBatch() { }

    public ImportBatch(Guid tenantId, string fileName)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        FileName = fileName.Trim();
        Status = ImportBatchStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public ImportBatchStatus Status { get; private set; }
    public long TotalRecords { get; private set; }
    public long ValidRecords { get; private set; }
    public long InvalidRecords { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void Complete(long totalRecords, long validRecords, long invalidRecords)
    {
        if (totalRecords < 0 || validRecords < 0 || invalidRecords < 0)
            throw new ArgumentOutOfRangeException(nameof(totalRecords), "Record counts cannot be negative.");
        if (validRecords + invalidRecords > totalRecords)
            throw new ArgumentException("Valid and invalid records cannot exceed total records.");

        TotalRecords = totalRecords;
        ValidRecords = validRecords;
        InvalidRecords = invalidRecords;
        Status = ImportBatchStatus.Completed;
    }

    public void MarkProcessing() => Status = ImportBatchStatus.Processing;
    public void MarkFailed() => Status = ImportBatchStatus.Failed;
}
