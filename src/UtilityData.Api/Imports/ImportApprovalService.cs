using Microsoft.EntityFrameworkCore;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Imports;

public sealed class ImportApprovalService(AppDbContext db, ILogger<ImportApprovalService> logger)
{
    private const int BatchSize = 500;

    public async Task<long> ApproveAsync(
        Guid batchId,
        Guid tenantId,
        Guid approvedByUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var batch = await db.ImportBatches
            .FirstOrDefaultAsync(x => x.Id == batchId && x.TenantId == tenantId, cancellationToken);

        if (batch is null)
            throw new KeyNotFoundException("Import batch was not found.");

        if (batch.Status == ImportBatchStatus.Approved)
            throw new InvalidOperationException("This import batch has already been approved.");

        if (batch.Status != ImportBatchStatus.Completed)
            throw new InvalidOperationException("Only completed import batches can be approved.");

        if (batch.ValidRecords == 0)
            throw new InvalidOperationException("This import has no valid records to commit.");

        long importedCount = 0;
        long lastRowNumber = 0;

        while (true)
        {
            var rows = await db.ImportRecords
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId
                    && x.ImportBatchId == batchId
                    && x.IsValid
                    && x.RowNumber > lastRowNumber)
                .OrderBy(x => x.RowNumber)
                .Take(BatchSize)
                .Select(x => new
                {
                    x.RowNumber,
                    x.AccountNumber,
                    x.CustomerName,
                    x.Email,
                    x.ServiceAddress
                })
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
                break;

            var accounts = rows.Select(row => new CustomerAccount(
                tenantId,
                row.AccountNumber!,
                row.CustomerName!,
                row.Email,
                row.ServiceAddress)).ToList();

            await db.CustomerAccounts.AddRangeAsync(accounts, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var account in accounts)
                db.Entry(account).State = EntityState.Detached;

            importedCount += rows.Count;
            lastRowNumber = rows[^1].RowNumber;
        }

        if (importedCount != batch.ValidRecords)
            throw new InvalidOperationException(
                $"Expected {batch.ValidRecords} valid rows, but found {importedCount} staged valid rows. No records were committed.");

        batch.Approve(approvedByUserId, importedCount);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Import batch {BatchId} approved by user {UserId}. {Count} customer accounts committed for tenant {TenantId}.",
            batchId, approvedByUserId, importedCount, tenantId);

        return importedCount;
    }
}
