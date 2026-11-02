using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Imports;

public sealed class CsvImportProcessor(AppDbContext db, ILogger<CsvImportProcessor> logger)
{
    private const int ChunkSize = 500;

    public async Task ProcessAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(x => x.Id == batchId, ct);
        if (batch is null || batch.Status != ImportBatchStatus.Pending) return;
        batch.MarkProcessing();
        await db.SaveChangesAsync(ct);

        long total = 0, valid = 0, invalid = 0;
        try
        {
            var existingList = await db.CustomerAccounts.AsNoTracking()
                .Where(x => x.TenantId == batch.TenantId).Select(x => x.AccountNumber).ToListAsync(ct);
            var seen = new HashSet<string>(existingList, StringComparer.OrdinalIgnoreCase);
            using var parser = new TextFieldParser(batch.StoredFilePath);
            parser.TextFieldType = FieldType.Delimited; parser.SetDelimiters(","); parser.HasFieldsEnclosedInQuotes = true;
            if (parser.EndOfData) throw new InvalidDataException("CSV file is empty.");
            var headers = parser.ReadFields() ?? [];
            var map = headers.Select((h, i) => new { Key = h.Trim(), Index = i })
                .ToDictionary(x => x.Key, x => x.Index, StringComparer.OrdinalIgnoreCase);
            foreach (var col in new[] { "AccountNumber", "CustomerName", "Email", "ServiceAddress" })
                if (!map.ContainsKey(col)) throw new InvalidDataException($"Missing required CSV column: {col}");

            var chunk = new List<ImportRecord>(ChunkSize);
            while (!parser.EndOfData)
            {
                ct.ThrowIfCancellationRequested();
                string[] row;
                try { row = parser.ReadFields() ?? []; }
                catch (MalformedLineException ex) { throw new InvalidDataException($"Malformed CSV near line {parser.ErrorLineNumber}.", ex); }

                total++;
                string? Val(string key) => map[key] < row.Length ? row[map[key]]?.Trim() : null;
                var account = Val("AccountNumber"); var name = Val("CustomerName");
                var email = Val("Email"); var address = Val("ServiceAddress");
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(account)) errors.Add("AccountNumber is required.");
                else if (account.Length > 80) errors.Add("AccountNumber exceeds 80 characters.");
                else if (!seen.Add(account)) errors.Add("AccountNumber already exists in this tenant or file.");
                if (string.IsNullOrWhiteSpace(name)) errors.Add("CustomerName is required.");
                else if (name.Length > 200) errors.Add("CustomerName exceeds 200 characters.");
                if (!string.IsNullOrWhiteSpace(email) &&
                    (email.Length > 320 || !new EmailAddressAttribute().IsValid(email)))
                    errors.Add("Email is invalid or exceeds 320 characters.");
                if (!string.IsNullOrWhiteSpace(address) && address.Length > 500)
                    errors.Add("ServiceAddress exceeds 500 characters.");

                var ok = errors.Count == 0;
                if (ok) valid++; else invalid++;
                chunk.Add(new ImportRecord(batch.TenantId, batch.Id, total, account, name, email, address,
                    ok, errors.Count == 0 ? null : string.Join(" ", errors)));
                if (chunk.Count == ChunkSize) await SaveChunkAsync(chunk, ct);
            }
            if (chunk.Count > 0) await SaveChunkAsync(chunk, ct);
            batch.Complete(total, valid, invalid);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Import {BatchId} complete: {Total} total, {Valid} valid, {Invalid} invalid",
                batchId, total, valid, invalid);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import {BatchId} failed", batchId);
            batch.MarkFailed(ex.Message);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task SaveChunkAsync(List<ImportRecord> chunk, CancellationToken ct)
    {
        await db.ImportRecords.AddRangeAsync(chunk, ct);
        await db.SaveChangesAsync(ct);
        chunk.Clear();
    }
}
