using Microsoft.EntityFrameworkCore;
using UtilityData.Domain.Entities;
using UtilityData.Infrastructure.Persistence;

namespace UtilityData.Api.Imports;

// Single-instance local/demo worker. Use atomic job claiming or a durable queue for multiple replicas.
public sealed class CsvImportWorker(IServiceScopeFactory scopes, ILogger<CsvImportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Guid? id;
                await using (var scope = scopes.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    id = await db.ImportBatches.AsNoTracking()
                        .Where(x => x.Status == ImportBatchStatus.Pending)
                        .OrderBy(x => x.CreatedAtUtc).Select(x => (Guid?)x.Id)
                        .FirstOrDefaultAsync(stoppingToken);
                }
                if (id is null) { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); continue; }
                await using var work = scopes.CreateAsyncScope();
                await work.ServiceProvider.GetRequiredService<CsvImportProcessor>()
                    .ProcessAsync(id.Value, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "CSV import worker failed.");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
