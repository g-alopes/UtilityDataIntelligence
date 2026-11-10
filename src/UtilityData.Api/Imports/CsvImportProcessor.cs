using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Imports;

/// <summary>Deterministic, mapping-version-pinned CSV/XLSX processor. Demo worker is single instance.</summary>
public sealed class CsvImportProcessor(AppDbContext db, ILogger<CsvImportProcessor> logger)
{
    private const int ChunkSize = 300;

    public async Task ProcessAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(x => x.Id == batchId, ct);
        if (batch is null || batch.Status != ImportBatchStatus.Pending) return;
        batch.MarkProcessing(); await db.SaveChangesAsync(ct);
        long total = 0, valid = 0, invalid = 0;
        try
        {
            if (batch.ImportMappingVersionId is null) throw new InvalidDataException("This import has no mapping version selected.");
            var version = await db.ImportMappingVersions.AsNoTracking().FirstOrDefaultAsync(x =>
                x.Id == batch.ImportMappingVersionId && x.TenantId == batch.TenantId, ct)
                ?? throw new InvalidDataException("The selected mapping version was not found for this tenant.");
            var mapping = await db.ImportMappings.AsNoTracking().FirstOrDefaultAsync(x =>
                x.Id == version.ImportMappingId && x.TenantId == batch.TenantId, ct)
                ?? throw new InvalidDataException("The selected import mapping was not found.");
            var columns = await db.ImportMappingColumns.AsNoTracking().Where(x =>
                x.TenantId == batch.TenantId && x.ImportMappingVersionId == version.Id).ToListAsync(ct);
            if (columns.Count == 0) throw new InvalidDataException("The selected mapping version has no configured columns.");
            var customIds = columns.Where(x => x.TargetKind == ImportMappingTargetKind.TenantCustomField)
                .Select(x => x.TenantCustomFieldId!.Value).Distinct().ToArray();
            var fields = await db.TenantCustomFields.AsNoTracking().Where(x =>
                x.TenantId == batch.TenantId && customIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
            if (fields.Count != customIds.Length) throw new InvalidDataException("A mapped custom field is inactive or does not belong to this tenant.");
            var existing = await db.CustomerAccounts.AsNoTracking().Where(x => x.TenantId == batch.TenantId)
                .Select(x => x.AccountNumber).ToListAsync(ct);
            var seen = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
            var headersAndRows = ReadRows(batch.StoredFilePath, mapping.FileFormat, version.WorksheetName, version.HeaderRowNumber);
            var headers = headersAndRows.Headers;
            var headerMap = headers.Select((h, i) => new { Key = h.Trim(), Index = i })
                .ToDictionary(x => x.Key, x => x.Index, StringComparer.OrdinalIgnoreCase);
            foreach (var col in columns)
                if (!headerMap.ContainsKey(col.SourceColumnName))
                    throw new InvalidDataException($"Mapped source column '{col.SourceColumnName}' was not found in the uploaded file.");

            var chunk = new List<(ImportRecord Record, List<ImportRecordCustomValue> Values)>(ChunkSize);
            foreach (var row in headersAndRows.Rows)
            {
                ct.ThrowIfCancellationRequested(); total++;
                string? Read(string source) { var i = headerMap[source]; return i < row.Length ? row[i]?.Trim() : null; }
                string? account = null, name = null, email = null, address = null;
                var errors = new List<string>(); var staged = new List<(Guid FieldId, (string? Text, long? Integer, decimal? Decimal, DateTime? Date, bool? Boolean) Value)>();
                foreach (var col in columns)
                {
                    var raw = Read(col.SourceColumnName);
                    var mappedFieldRequired = col.TargetKind == ImportMappingTargetKind.TenantCustomField
                        && fields.TryGetValue(col.TenantCustomFieldId!.Value, out var requiredField) && requiredField.IsRequired;
                    if ((col.IsRequired || mappedFieldRequired) && string.IsNullOrWhiteSpace(raw))
                        errors.Add($"{col.SourceColumnName} is required.");
                    if (col.TargetKind == ImportMappingTargetKind.CoreField)
                    {
                        switch (col.TargetFieldKey)
                        {
                            case "AccountNumber": account = raw; break;
                            case "CustomerName": name = raw; break;
                            case "Email": email = raw; break;
                            case "ServiceAddress": address = raw; break;
                            default: errors.Add($"Unsupported core field '{col.TargetFieldKey}'."); break;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(raw)) continue;
                        var field = fields[col.TenantCustomFieldId!.Value];
                        if (TryParseCustom(raw!, field.DataType, version, out var typed, out var parseError))
                            staged.Add((field.Id, typed));
                        else errors.Add($"{field.Label}: {parseError}");
                    }
                }
                if (string.IsNullOrWhiteSpace(account)) errors.Add("AccountNumber is required.");
                else if (account.Length > 80) errors.Add("AccountNumber exceeds 80 characters.");
                else if (!seen.Add(account)) errors.Add("AccountNumber already exists in this tenant or file.");
                if (string.IsNullOrWhiteSpace(name)) errors.Add("CustomerName is required.");
                else if (name.Length > 200) errors.Add("CustomerName exceeds 200 characters.");
                if (!string.IsNullOrWhiteSpace(email) && (email.Length > 320 || !new EmailAddressAttribute().IsValid(email))) errors.Add("Email is invalid or exceeds 320 characters.");
                if (!string.IsNullOrWhiteSpace(address) && address.Length > 500) errors.Add("ServiceAddress exceeds 500 characters.");
                var ok = errors.Count == 0; if (ok) valid++; else invalid++;
                var record = new ImportRecord(batch.TenantId, batch.Id, total, account, name, email, address, ok,
                    errors.Count == 0 ? null : string.Join(" ", errors));
                // Recreate staged values after ImportRecord has generated its identity.
                if (ok && staged.Count > 0)
                {
                    var values = staged.Select(v => new ImportRecordCustomValue(batch.TenantId, record.Id, v.FieldId,
                        v.Value.Text, v.Value.Integer, v.Value.Decimal, v.Value.Date, v.Value.Boolean)).ToList();
                    chunk.Add((record, values));
                }
                else chunk.Add((record, []));
                if (chunk.Count >= ChunkSize) await SaveChunkAsync(chunk, ct);
            }
            if (chunk.Count > 0) await SaveChunkAsync(chunk, ct);
            batch.Complete(total, valid, invalid); await db.SaveChangesAsync(ct);
            logger.LogInformation("Import {BatchId} completed: {Total} total, {Valid} valid, {Invalid} invalid", batchId, total, valid, invalid);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import {BatchId} failed", batchId);
            // Avoid partial staged rows being accidentally approved after a parser failure.
            await db.ImportRecordCustomValues.Where(x => x.TenantId == batch.TenantId &&
                db.ImportRecords.Any(r => r.Id == x.ImportRecordId && r.ImportBatchId == batchId)).ExecuteDeleteAsync(CancellationToken.None);
            await db.ImportRecords.Where(x => x.TenantId == batch.TenantId && x.ImportBatchId == batchId).ExecuteDeleteAsync(CancellationToken.None);
            batch.MarkFailed(ex.Message); await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private static (string[] Headers, IEnumerable<string[]> Rows) ReadRows(string path, ImportFileFormat format, string? worksheet, int headerRow)
    {
        if (format == ImportFileFormat.Csv)
        {
            var parser = new TextFieldParser(path); parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",", ";", "\t", "|"); parser.HasFieldsEnclosedInQuotes = true;
            if (parser.EndOfData) { parser.Dispose(); throw new InvalidDataException("CSV file is empty."); }
            for (var i = 1; i < headerRow; i++) { if (parser.EndOfData) { parser.Dispose(); throw new InvalidDataException("Configured header row is beyond end of file."); } parser.ReadFields(); }
            var headers = parser.ReadFields() ?? []; return (headers, CsvRows(parser));
        }
        using var workbook = new XLWorkbook(path);
        var sheet = worksheet is null ? workbook.Worksheets.FirstOrDefault() : workbook.Worksheets.FirstOrDefault(x => x.Name == worksheet);
        if (sheet is null) throw new InvalidDataException("Configured worksheet was not found in the uploaded workbook.");
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        if (headerRow > lastRow || lastCol == 0) throw new InvalidDataException("Configured header row is empty or outside the worksheet.");
        var h = Enumerable.Range(1, lastCol).Select(c => sheet.Cell(headerRow, c).GetString().Trim()).ToArray();
        var rows = new List<string[]>();
        for (var r = headerRow + 1; r <= lastRow; r++) rows.Add(Enumerable.Range(1, lastCol).Select(c => sheet.Cell(r, c).GetFormattedString()).ToArray());
        return (h, rows);
    }
    private static IEnumerable<string[]> CsvRows(TextFieldParser parser)
    { using (parser) { while (!parser.EndOfData) yield return parser.ReadFields() ?? []; } }

    private static bool TryParseCustom(string raw, CustomFieldDataType type, ImportMappingVersion version,
        out (string? Text, long? Integer, decimal? Decimal, DateTime? Date, bool? Boolean) value, out string error)
    {
        value = default; error = "Value has an invalid format.";
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        if (!string.IsNullOrEmpty(version.DecimalSeparator)) culture.NumberFormat.NumberDecimalSeparator = version.DecimalSeparator;
        if (!string.IsNullOrEmpty(version.ThousandsSeparator)) culture.NumberFormat.NumberGroupSeparator = version.ThousandsSeparator;
        switch (type)
        {
            case CustomFieldDataType.Text: if (raw.Length > 4000) { error = "Text exceeds 4000 characters."; return false; } value = (raw, null, null, null, null); return true;
            case CustomFieldDataType.Integer: if (long.TryParse(raw, NumberStyles.Integer, culture, out var l)) { value = (null, l, null, null, null); return true; } return false;
            case CustomFieldDataType.Decimal: if (decimal.TryParse(raw, NumberStyles.Number, culture, out var d)) { value = (null, null, d, null, null); return true; } return false;
            case CustomFieldDataType.Date:
                var okDate = !string.IsNullOrWhiteSpace(version.DateFormat)
                    ? DateTime.TryParseExact(raw, version.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateExact)
                    : DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateExact);
                if (okDate) { value = (null, null, null, dateExact, null); return true; } return false;
            case CustomFieldDataType.Boolean:
                if (bool.TryParse(raw, out var b)) { value = (null, null, null, null, b); return true; }
                if (raw == "1" || raw.Equals("yes", StringComparison.OrdinalIgnoreCase) || raw.Equals("sim", StringComparison.OrdinalIgnoreCase)) { value = (null, null, null, null, true); return true; }
                if (raw == "0" || raw.Equals("no", StringComparison.OrdinalIgnoreCase) || raw.Equals("não", StringComparison.OrdinalIgnoreCase)) { value = (null, null, null, null, false); return true; }
                return false;
            default: error = "Unsupported custom field type."; return false;
        }
    }
    private async Task SaveChunkAsync(List<(ImportRecord Record, List<ImportRecordCustomValue> Values)> chunk, CancellationToken ct)
    {
        await db.ImportRecords.AddRangeAsync(chunk.Select(x => x.Record), ct);
        await db.ImportRecordCustomValues.AddRangeAsync(chunk.SelectMany(x => x.Values), ct);
        await db.SaveChangesAsync(ct); chunk.Clear();
    }
}
