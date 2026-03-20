using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.RiskCountries.AnyAsync())
            return;

        db.RiskCountries.AddRange(
            new RiskCountryEntity { CountryCode = "CD", CountryName = "Democratic Republic of the Congo", RiskLevel = "HIGH", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "RW", CountryName = "Rwanda", RiskLevel = "HIGH", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "BI", CountryName = "Burundi", RiskLevel = "HIGH", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "UG", CountryName = "Uganda", RiskLevel = "HIGH", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "TZ", CountryName = "Tanzania", RiskLevel = "MEDIUM", Source = "OECD Annex II" },
            new RiskCountryEntity { CountryCode = "KE", CountryName = "Kenya", RiskLevel = "LOW", Source = "OECD Annex II" }
        );

        db.RmapSmelters.AddRange(
            new RmapSmelterEntity { SmelterId = "CID001100", SmelterName = "Wolfram Bergbau und Hutten AG", Country = "AT", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 6, 15), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID002158", SmelterName = "Global Tungsten & Powders Corp.", Country = "US", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 3, 10), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID002082", SmelterName = "Xiamen Tungsten Co., Ltd.", Country = "CN", ConformanceStatus = "ACTIVE_PARTICIPATING", LastAuditDate = new DateOnly(2025, 8, 22), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID000999", SmelterName = "Unaudited Smelter Example", Country = "XX", ConformanceStatus = "NON_CONFORMANT", LastAuditDate = null, LoadedAt = DateTime.UtcNow }
        );

        db.SanctionedEntities.AddRange(
            new SanctionedEntityEntity { Id = Guid.NewGuid(), EntityName = "Sanctioned Mining Corp", EntityType = "ORGANIZATION", Source = "UN Security Council", LoadedAt = DateTime.UtcNow },
            new SanctionedEntityEntity { Id = Guid.NewGuid(), EntityName = "Restricted Trader LLC", EntityType = "ORGANIZATION", Source = "EU Sanctions List", LoadedAt = DateTime.UtcNow }
        );

        var tenant = new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = "Pilot Tenant",
            SchemaPrefix = "tenant_pilot",
            Status = "ACTIVE"
        };
        db.Tenants.Add(tenant);

        await db.SaveChangesAsync();
    }
}
