using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Identity;

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

            // Identity tables (separate schema)
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await identityDb.Database.MigrateAsync(stoppingToken);

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(stoppingToken);

            // Fix: add ParentBatchId if missing (migration was recorded but column never created)
            await db.Database.ExecuteSqlRawAsync("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'batches' AND column_name = 'ParentBatchId'
                    ) THEN
                        ALTER TABLE batches ADD COLUMN "ParentBatchId" uuid;
                        CREATE INDEX "IX_batches_ParentBatchId" ON batches ("ParentBatchId");
                        ALTER TABLE batches ADD CONSTRAINT "FK_batches_batches_ParentBatchId"
                            FOREIGN KEY ("ParentBatchId") REFERENCES batches("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """, stoppingToken);

            // One-time: migrate existing Entra users to pending| so they can register
            var entraUsers = await db.Database.ExecuteSqlRawAsync("""
                UPDATE users
                SET identity_user_id = 'pending|' || "Id"::text
                WHERE identity_user_id IS NOT NULL
                  AND identity_user_id NOT LIKE 'pending|%'
                  AND NOT EXISTS (
                    SELECT 1 FROM identity."AspNetUsers" a WHERE a."Id" = users.identity_user_id
                  )
                """, stoppingToken);
            if (entraUsers > 0)
                logger.LogInformation("Migrated {Count} existing users to pending| for Identity registration", entraUsers);

            await SeedData.SeedAsync(db);

            // Ensure platform admin exists and has correct role
            var platformAdmin = await db.Users.FirstOrDefaultAsync(
                u => u.Email == "julianshaw2000@gmail.com", stoppingToken);
            if (platformAdmin is not null && platformAdmin.Role != "PLATFORM_ADMIN")
            {
                platformAdmin.Role = "PLATFORM_ADMIN";
                platformAdmin.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);
                logger.LogInformation("Promoted julianshaw2000@gmail.com to PLATFORM_ADMIN");
            }

            // Ensure demo users exist (idempotent)
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Status == "ACTIVE", stoppingToken);
            if (tenant is not null)
            {
                var demoUsers = new[]
                {
                    ("buyer@auditraks.com", "Klaus Steinberger (Wolfram Bergbau)", "BUYER"),
                    ("admin@auditraks.com", "Marie Uwimana (Compliance Director)", "TENANT_ADMIN"),
                };
                foreach (var (email, name, role) in demoUsers)
                {
                    if (!await db.Users.AnyAsync(u => u.Email == email, stoppingToken))
                    {
                        var id = Guid.NewGuid();
                        db.Users.Add(new Tungsten.Api.Infrastructure.Persistence.Entities.UserEntity
                        {
                            Id = id,
                            IdentityUserId = $"pending|{id}",
                            Email = email,
                            DisplayName = name,
                            Role = role,
                            TenantId = tenant.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        });
                        logger.LogInformation("Created demo user {Email} ({Role})", email, role);
                    }
                }
                await db.SaveChangesAsync(stoppingToken);
            }

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
