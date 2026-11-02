namespace UtilityData.Domain.Entities;

public enum ImportBatchStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Approved = 4
}

public sealed class ImportBatch
{
    private ImportBatch() { }

    public ImportBatch(Guid tenantId, Guid createdByUserId, string fileName, string storedFilePath)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("Creator ID is required.", nameof(createdByUserId));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(storedFilePath)) throw new ArgumentException("Stored path is required.", nameof(storedFilePath));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        CreatedByUserId = createdByUserId;
        FileName = Path.GetFileName(fileName);
        StoredFilePath = storedFilePath;
        Status = ImportBatchStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StoredFilePath { get; private set; } = string.Empty;
    public ImportBatchStatus Status { get; private set; }
    public long TotalRecords { get; private set; }
    public long ValidRecords { get; private set; }
    public long InvalidRecords { get; private set; }
    public long ApprovedRecordCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }

    public void MarkProcessing() => Status = ImportBatchStatus.Processing;

    public void Complete(long total, long valid, long invalid)
    {
        if (total < 0 || valid < 0 || invalid < 0 || valid + invalid != total)
            throw new ArgumentException("Import counters are inconsistent.");

        TotalRecords = total;
        ValidRecords = valid;
        InvalidRecords = invalid;
        Status = ImportBatchStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Approve(Guid approvedByUserId, long approvedRecordCount)
    {
        if (Status != ImportBatchStatus.Completed)
            throw new InvalidOperationException("Only completed imports can be approved.");
        if (approvedByUserId == Guid.Empty)
            throw new ArgumentException("Approver user ID is required.", nameof(approvedByUserId));
        if (approvedRecordCount < 1 || approvedRecordCount > ValidRecords)
            throw new ArgumentOutOfRangeException(nameof(approvedRecordCount));

        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = DateTime.UtcNow;
        ApprovedRecordCount = approvedRecordCount;
        Status = ImportBatchStatus.Approved;
    }

    public void MarkFailed(string message)
    {
        ErrorMessage = string.IsNullOrWhiteSpace(message)
            ? "Import failed."
            : message[..Math.Min(message.Length, 1000)];
        Status = ImportBatchStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
