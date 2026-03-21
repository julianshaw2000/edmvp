using Microsoft.EntityFrameworkCore;

namespace Tungsten.Api.Infrastructure.Persistence;

/// <summary>
/// Runs EF migrations and seed data in the background after the server starts,
/// so Kestrel can accept requests (including health checks) immediately.
/// </summary>
public sealed class DatabaseMigrationService(IServiceProvider services, ILogger<DatabaseMigrationService> logger) : BackgroundService
{
    public static volatile bool IsReady;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting database migration...");
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(stoppingToken);
            await SeedData.SeedAsync(db);

            try
            {
                await SeedData.SeedDemoBatchesIfNeededAsync(db);
            }
            catch (Exception seedEx)
            {
                logger.LogWarning(seedEx, "Demo seed data failed (non-fatal). App will continue.");
            }

            IsReady = true;
            logger.LogInformation("Database migration completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed.");
            throw;
        }
    }
}
