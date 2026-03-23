using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<BatchEntity> Batches => Set<BatchEntity>();
    public DbSet<CustodyEventEntity> CustodyEvents => Set<CustodyEventEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<ComplianceCheckEntity> ComplianceChecks => Set<ComplianceCheckEntity>();
    public DbSet<GeneratedDocumentEntity> GeneratedDocuments => Set<GeneratedDocumentEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<JobEntity> Jobs => Set<JobEntity>();
    public DbSet<RmapSmelterEntity> RmapSmelters => Set<RmapSmelterEntity>();
    public DbSet<RiskCountryEntity> RiskCountries => Set<RiskCountryEntity>();
    public DbSet<SanctionedEntityEntity> SanctionedEntities => Set<SanctionedEntityEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
