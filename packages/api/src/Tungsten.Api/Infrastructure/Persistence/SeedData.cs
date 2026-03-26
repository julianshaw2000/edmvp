using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            // Tungsten smelters
            new RmapSmelterEntity { SmelterId = "CID001100", SmelterName = "Wolfram Bergbau und Hutten AG", Country = "AT", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 6, 15), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID002158", SmelterName = "Global Tungsten & Powders Corp.", Country = "US", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 3, 10), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID002082", SmelterName = "Xiamen Tungsten Co., Ltd.", Country = "CN", ConformanceStatus = "ACTIVE_PARTICIPATING", LastAuditDate = new DateOnly(2025, 8, 22), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID000999", SmelterName = "Unaudited Smelter Example", Country = "XX", ConformanceStatus = "NON_CONFORMANT", LastAuditDate = null, LoadedAt = DateTime.UtcNow },
            // Tin smelters
            new RmapSmelterEntity { SmelterId = "CID001070", SmelterName = "Malaysia Smelting Corporation", Country = "MY", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 5, 20), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID000468", SmelterName = "PT Timah Tbk", Country = "ID", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 7, 15), LoadedAt = DateTime.UtcNow },
            // Tantalum smelters
            new RmapSmelterEntity { SmelterId = "CID000211", SmelterName = "Global Advanced Metals", Country = "AU", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 4, 10), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID002544", SmelterName = "KEMET Blue Powder", Country = "US", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 9, 5), LoadedAt = DateTime.UtcNow },
            // Gold refiners
            new RmapSmelterEntity { SmelterId = "CID000058", SmelterName = "Argor-Heraeus SA", Country = "CH", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 2, 28), LoadedAt = DateTime.UtcNow },
            new RmapSmelterEntity { SmelterId = "CID000694", SmelterName = "PAMP SA", Country = "CH", ConformanceStatus = "CONFORMANT", LastAuditDate = new DateOnly(2025, 8, 18), LoadedAt = DateTime.UtcNow }
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

        // Platform admin — always ensure this account exists
        var platformAdmin = new UserEntity
        {
            Id = Guid.NewGuid(),
            EntraOid = "pending|platform-admin",
            Email = "julianshaw2000@gmail.com",
            DisplayName = "Julian Shaw",
            Role = "PLATFORM_ADMIN",
            TenantId = tenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Users.Add(platformAdmin);

        await db.SaveChangesAsync();

        // --- Demo data for investor walkthrough ---
        await SeedDemoBatchesAsync(db, tenant.Id);
    }

    // Seed demo batches independently — runs even if reference data already exists
    public static async Task SeedDemoBatchesIfNeededAsync(AppDbContext db)
    {
        if (await db.Batches.AnyAsync())
            return;
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Status == "ACTIVE");
        if (tenant is null)
            return;
        await SeedDemoBatchesAsync(db, tenant.Id);
    }

    private static async Task SeedDemoBatchesAsync(AppDbContext db, Guid tenantId)
    {
        if (await db.Batches.AnyAsync())
            return;

        // Find or create a demo supplier user for batch ownership
        var demoUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "supplier@auditraks.com");
        if (demoUser is null)
        {
            demoUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                EntraOid = $"seed|demo-supplier-{Guid.NewGuid():N}",
                Email = "supplier@auditraks.com",
                DisplayName = "Demo Supplier (Nyungwe Mining Co.)",
                Role = "SUPPLIER",
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(demoUser);
            await db.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;

        // ===== Batch W-2026-041: Rwanda, Nyungwe Mine, 450kg, COMPLIANT, 6 events =====
        var batch041 = new BatchEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BatchNumber = "W-2026-041",
            MineralType = "Tungsten (Wolframite)",
            OriginCountry = "RW",
            OriginMine = "Nyungwe Mine",
            WeightKg = 450m,
            Status = "COMPLETED",
            ComplianceStatus = "COMPLIANT",
            CreatedBy = demoUser.Id,
            CreatedAt = now.AddDays(-30),
            UpdatedAt = now.AddDays(-2)
        };
        db.Batches.Add(batch041);

        var events041 = CreateBatch041Events(batch041.Id, tenantId, demoUser.Id, now);
        db.CustodyEvents.AddRange(events041);

        // ===== Batch W-2026-038: DRC, Bisie Mine, 780kg, FLAGGED, 4 events =====
        var batch038 = new BatchEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BatchNumber = "W-2026-038",
            MineralType = "Tungsten (Wolframite)",
            OriginCountry = "CD",
            OriginMine = "Bisie Mine",
            WeightKg = 780m,
            Status = "ACTIVE",
            ComplianceStatus = "FLAGGED",
            CreatedBy = demoUser.Id,
            CreatedAt = now.AddDays(-45),
            UpdatedAt = now.AddDays(-10)
        };
        db.Batches.Add(batch038);

        var events038 = CreateBatch038Events(batch038.Id, tenantId, demoUser.Id, now);
        db.CustodyEvents.AddRange(events038);

        // ===== Batch W-2026-045: Bolivia, Huanuni Mine, 220kg, PENDING, 0 events =====
        var batch045 = new BatchEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BatchNumber = "W-2026-045",
            MineralType = "Tungsten (Wolframite)",
            OriginCountry = "BO",
            OriginMine = "Huanuni Mine",
            WeightKg = 220m,
            Status = "CREATED",
            ComplianceStatus = "PENDING",
            CreatedBy = demoUser.Id,
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-3)
        };
        db.Batches.Add(batch045);

        // ===== Batch W-2026-035: Rwanda, Rutongo Mine, 320kg, COMPLIANT, 5 events =====
        var batch035 = new BatchEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BatchNumber = "W-2026-035",
            MineralType = "Tungsten (Cassiterite)",
            OriginCountry = "RW",
            OriginMine = "Rutongo Mine",
            WeightKg = 320m,
            Status = "COMPLETED",
            ComplianceStatus = "COMPLIANT",
            CreatedBy = demoUser.Id,
            CreatedAt = now.AddDays(-60),
            UpdatedAt = now.AddDays(-15)
        };
        db.Batches.Add(batch035);

        var events035 = CreateBatch035Events(batch035.Id, tenantId, demoUser.Id, now);
        db.CustodyEvents.AddRange(events035);

        await db.SaveChangesAsync();

        // --- Compliance checks ---
        SeedComplianceChecks041(db, batch041.Id, tenantId, events041, now);
        SeedComplianceChecks038(db, batch038.Id, tenantId, events038, now);
        SeedComplianceChecks035(db, batch035.Id, tenantId, events035, now);

        await db.SaveChangesAsync();
    }

    // ===== W-2026-041: Full mine-to-refinery journey (6 events) =====
    private static List<CustodyEventEntity> CreateBatch041Events(Guid batchId, Guid tenantId, Guid userId, DateTime now)
    {
        var events = new List<CustodyEventEntity>();
        string? previousHash = null;

        // Event 1: Mine Extraction
        var e1 = CreateEvent(batchId, tenantId, userId,
            "MINE_EXTRACTION", now.AddDays(-28),
            "Nyungwe Mine, Nyamagabe District, Rwanda", "-2.4305, 29.2583",
            "Jean-Baptiste Habimana", null,
            "Initial extraction of wolframite ore from Nyungwe Mine shaft 3, artisanal mining cooperative",
            JsonSerializer.SerializeToElement(new { gpsCoordinates = "-2.4305, 29.2583", mineOperatorIdentity = "Nyungwe Mining Cooperative Ltd.", mineralogicalCertificateRef = "NMC-2026-0187" }),
            previousHash);
        events.Add(e1);
        previousHash = e1.Sha256Hash;

        // Event 2: Lab Assay
        var e2 = CreateEvent(batchId, tenantId, userId,
            "LABORATORY_ASSAY", now.AddDays(-25),
            "SGS Minerals Laboratory, Kigali, Rwanda", "-1.9403, 29.8739",
            "Dr. Marie Uwimana", null,
            "Laboratory analysis confirming 65.2% WO3 content, wolframite mineral identification verified",
            JsonSerializer.SerializeToElement(new { laboratoryName = "SGS Minerals Laboratory Kigali", assayMethod = "XRF Spectroscopy + ICP-OES", tungstenContentPct = "65.2", assayCertificateRef = "SGS-RW-2026-04412" }),
            previousHash);
        events.Add(e2);
        previousHash = e2.Sha256Hash;

        // Event 3: Concentration
        var e3 = CreateEvent(batchId, tenantId, userId,
            "CONCENTRATION", now.AddDays(-21),
            "Wolfram Mining & Processing, Gisenyi, Rwanda", "-1.7030, 29.2564",
            "Emmanuel Nsengiyumva", null,
            "Gravity separation and magnetic concentration, concentrate grade upgraded to 71% WO3",
            JsonSerializer.SerializeToElement(new { facilityName = "Wolfram Mining & Processing Gisenyi", processDescription = "Gravity separation followed by magnetic concentration", inputWeightKg = "450", outputWeightKg = "385", concentrationRatio = "1.17" }),
            previousHash);
        events.Add(e3);
        previousHash = e3.Sha256Hash;

        // Event 4: Trading Transfer
        var e4 = CreateEvent(batchId, tenantId, userId,
            "TRADING_TRANSFER", now.AddDays(-16),
            "Kigali Export Processing Zone, Rwanda", "-1.9623, 30.0644",
            "Patrick Mugisha", null,
            "Transfer of ownership from mining cooperative to licensed mineral trader for export",
            JsonSerializer.SerializeToElement(new { sellerIdentity = "Nyungwe Mining Cooperative Ltd.", buyerIdentity = "Great Lakes Minerals Trading SA", transferDate = "2026-03-05", contractReference = "GLM-NMC-2026-0092" }),
            previousHash);
        events.Add(e4);
        previousHash = e4.Sha256Hash;

        // Event 5: Primary Processing (Smelting)
        var e5 = CreateEvent(batchId, tenantId, userId,
            "PRIMARY_PROCESSING", now.AddDays(-8),
            "Wolfram Bergbau und Hutten AG, St. Martin, Austria", "46.8465, 14.3483",
            "Klaus Steinberger", "CID001100",
            "Smelting of wolframite concentrate into APT (ammonium paratungstate) intermediate product",
            JsonSerializer.SerializeToElement(new { smelterId = "CID001100", processType = "Hydrometallurgical APT production", inputWeightKg = "385", outputWeightKg = "310" }),
            previousHash);
        events.Add(e5);
        previousHash = e5.Sha256Hash;

        // Event 6: Export Shipment
        var e6 = CreateEvent(batchId, tenantId, userId,
            "EXPORT_SHIPMENT", now.AddDays(-4),
            "Port of Mombasa, Kenya", "-4.0435, 39.6682",
            "Grace Wanjiku", null,
            "Export shipment of processed tungsten concentrate from East Africa to European smelter",
            JsonSerializer.SerializeToElement(new { originCountry = "RW", destinationCountry = "AT", transportMode = "Sea freight via Mombasa", exportPermitRef = "RW-EXP-2026-00341" }),
            previousHash);
        events.Add(e6);

        return events;
    }

    // ===== W-2026-038: DRC batch, FLAGGED (4 events) =====
    private static List<CustodyEventEntity> CreateBatch038Events(Guid batchId, Guid tenantId, Guid userId, DateTime now)
    {
        var events = new List<CustodyEventEntity>();
        string? previousHash = null;

        // Event 1: Mine Extraction
        var e1 = CreateEvent(batchId, tenantId, userId,
            "MINE_EXTRACTION", now.AddDays(-42),
            "Bisie Mine, Walikale Territory, North Kivu, DRC", "-1.3522, 27.7835",
            "Amani Kakule", null,
            "Extraction of wolframite ore from Bisie Mine main deposit, semi-mechanized operation",
            JsonSerializer.SerializeToElement(new { gpsCoordinates = "-1.3522, 27.7835", mineOperatorIdentity = "Bisie Mining SA", mineralogicalCertificateRef = "BMS-2026-0054" }),
            previousHash);
        events.Add(e1);
        previousHash = e1.Sha256Hash;

        // Event 2: Lab Assay
        var e2 = CreateEvent(batchId, tenantId, userId,
            "LABORATORY_ASSAY", now.AddDays(-38),
            "Bureau Veritas Laboratory, Goma, DRC", "-1.6771, 29.2383",
            "Prof. Augustin Banywesize", null,
            "Assay of DRC wolframite ore, 58.7% WO3 content confirmed",
            JsonSerializer.SerializeToElement(new { laboratoryName = "Bureau Veritas Goma Laboratory", assayMethod = "XRF Spectroscopy", tungstenContentPct = "58.7", assayCertificateRef = "BV-CD-2026-01198" }),
            previousHash);
        events.Add(e2);
        previousHash = e2.Sha256Hash;

        // Event 3: Concentration
        var e3 = CreateEvent(batchId, tenantId, userId,
            "CONCENTRATION", now.AddDays(-32),
            "Goma Processing Facility, North Kivu, DRC", "-1.6810, 29.2290",
            "Prosper Muhindo", null,
            "Concentration processing of Bisie wolframite, upgraded to 64% WO3",
            JsonSerializer.SerializeToElement(new { facilityName = "Goma Mineral Processing SARL", processDescription = "Gravity table concentration", inputWeightKg = "780", outputWeightKg = "650", concentrationRatio = "1.20" }),
            previousHash);
        events.Add(e3);
        previousHash = e3.Sha256Hash;

        // Event 4: Trading Transfer
        var e4 = CreateEvent(batchId, tenantId, userId,
            "TRADING_TRANSFER", now.AddDays(-26),
            "Goma Border Trading Post, DRC", "-1.6795, 29.2210",
            "Fidele Nzabandora", null,
            "Transfer to licensed cross-border mineral trader pending further compliance review",
            JsonSerializer.SerializeToElement(new { sellerIdentity = "Bisie Mining SA", buyerIdentity = "Kivu Minerals Export SARL", transferDate = "2026-02-23", contractReference = "KME-BMS-2026-0031" }),
            previousHash);
        events.Add(e4);

        return events;
    }

    // ===== W-2026-035: Rwanda, Rutongo Mine, COMPLIANT (5 events) =====
    private static List<CustodyEventEntity> CreateBatch035Events(Guid batchId, Guid tenantId, Guid userId, DateTime now)
    {
        var events = new List<CustodyEventEntity>();
        string? previousHash = null;

        // Event 1: Mine Extraction
        var e1 = CreateEvent(batchId, tenantId, userId,
            "MINE_EXTRACTION", now.AddDays(-55),
            "Rutongo Mine, Rulindo District, Rwanda", "-1.7762, 29.8641",
            "Theogene Ndayisaba", null,
            "Extraction of cassiterite-wolframite ore from Rutongo underground mine level 4",
            JsonSerializer.SerializeToElement(new { gpsCoordinates = "-1.7762, 29.8641", mineOperatorIdentity = "Rutongo Mines Ltd.", mineralogicalCertificateRef = "RML-2026-0223" }),
            previousHash);
        events.Add(e1);
        previousHash = e1.Sha256Hash;

        // Event 2: Lab Assay
        var e2 = CreateEvent(batchId, tenantId, userId,
            "LABORATORY_ASSAY", now.AddDays(-51),
            "Rwanda Mines, Petroleum & Gas Board Lab, Kigali", "-1.9474, 30.0617",
            "Dr. Claudine Mukamana", null,
            "Government laboratory analysis confirming 62.8% WO3, trace tin content at 3.1%",
            JsonSerializer.SerializeToElement(new { laboratoryName = "RMPGB Central Laboratory", assayMethod = "ICP-OES with XRD mineral phase analysis", tungstenContentPct = "62.8", assayCertificateRef = "RMPGB-2026-00891" }),
            previousHash);
        events.Add(e2);
        previousHash = e2.Sha256Hash;

        // Event 3: Concentration
        var e3 = CreateEvent(batchId, tenantId, userId,
            "CONCENTRATION", now.AddDays(-45),
            "Rutongo Processing Plant, Rulindo, Rwanda", "-1.7801, 29.8598",
            "Innocent Habiyaremye", null,
            "On-site gravity and flotation concentration, upgraded to 68% WO3",
            JsonSerializer.SerializeToElement(new { facilityName = "Rutongo Mines Processing Plant", processDescription = "Jig and shaking table concentration with flotation", inputWeightKg = "320", outputWeightKg = "275", concentrationRatio = "1.16" }),
            previousHash);
        events.Add(e3);
        previousHash = e3.Sha256Hash;

        // Event 4: Trading Transfer
        var e4 = CreateEvent(batchId, tenantId, userId,
            "TRADING_TRANSFER", now.AddDays(-35),
            "Kigali Mineral Exchange, Rwanda", "-1.9536, 30.0606",
            "Alphonse Niyonzima", null,
            "Sale of concentrated tungsten to international commodity trader",
            JsonSerializer.SerializeToElement(new { sellerIdentity = "Rutongo Mines Ltd.", buyerIdentity = "Traxys Europe SA", transferDate = "2026-02-14", contractReference = "TRX-RML-2026-0018" }),
            previousHash);
        events.Add(e4);
        previousHash = e4.Sha256Hash;

        // Event 5: Export Shipment
        var e5 = CreateEvent(batchId, tenantId, userId,
            "EXPORT_SHIPMENT", now.AddDays(-28),
            "Kigali International Airport, Rwanda", "-1.9686, 30.1395",
            "Diane Mukeshimana", null,
            "Air freight export of tungsten concentrate to Global Tungsten & Powders facility",
            JsonSerializer.SerializeToElement(new { originCountry = "RW", destinationCountry = "US", transportMode = "Air freight via Kigali International Airport", exportPermitRef = "RW-EXP-2026-00298" }),
            previousHash);
        events.Add(e5);

        return events;
    }

    private static CustodyEventEntity CreateEvent(
        Guid batchId, Guid tenantId, Guid userId,
        string eventType, DateTime eventDate,
        string location, string? gpsCoordinates,
        string actorName, string? smelterId,
        string description, JsonElement metadata,
        string? previousHash)
    {
        var normalizedDate = eventDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        var metadataString = JsonSerializer.Serialize(metadata);

        var hash = ComputeEventHash(eventType, normalizedDate, batchId, location,
            actorName, smelterId, description, metadataString, previousHash);

        return new CustodyEventEntity
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            TenantId = tenantId,
            EventType = eventType,
            IdempotencyKey = $"seed-{batchId:N}-{eventType}-{normalizedDate}",
            EventDate = eventDate,
            Location = location,
            GpsCoordinates = gpsCoordinates,
            ActorName = actorName,
            SmelterId = smelterId,
            Description = description,
            Metadata = metadata,
            SchemaVersion = 1,
            IsCorrection = false,
            Sha256Hash = hash,
            PreviousEventHash = previousHash,
            CreatedBy = userId,
            CreatedAt = eventDate
        };
    }

    private static string ComputeEventHash(
        string eventType, string eventDate, Guid batchId,
        string location, string actorName, string? smelterId,
        string description, string metadata, string? previousEventHash)
    {
        var fields = new SortedDictionary<string, string>
        {
            ["actor_name"] = actorName,
            ["batch_id"] = batchId.ToString(),
            ["description"] = description,
            ["event_date"] = eventDate,
            ["event_type"] = eventType,
            ["location"] = location,
            ["metadata"] = metadata,
            ["previous_event_hash"] = previousEventHash ?? "",
            ["smelter_id"] = smelterId ?? "",
        };

        var canonical = JsonSerializer.Serialize(fields, new JsonSerializerOptions { WriteIndented = false });
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexStringLower(hashBytes);
    }

    // ===== Compliance Checks =====

    private static void SeedComplianceChecks041(AppDbContext db, Guid batchId, Guid tenantId, List<CustodyEventEntity> events, DateTime now)
    {
        // W-2026-041: 5 checks all PASS (RMAP, OECD_DDG, SANCTIONS, MASS_BALANCE, SEQUENCE_CHECK)
        var lastEvent = events[^1];
        var checkedAt = now.AddDays(-3);

        db.ComplianceChecks.AddRange(
            CreateCheck(lastEvent.Id, batchId, tenantId, "RMAP", "PASS", checkedAt,
                new { result = "Smelter CID001100 (Wolfram Bergbau und Hutten AG) is RMAP CONFORMANT", smelterId = "CID001100", conformanceStatus = "CONFORMANT" }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "OECD_DDG", "PASS", checkedAt,
                new { result = "OECD DDG check result: PASS. Sub-checks: origin_risk=PASS; sanctions=PASS; transport_route=PASS", originCountry = "RW", riskLevel = "HIGH", mitigated = true }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "SANCTIONS", "PASS", checkedAt,
                new { result = "No sanctioned entities found in custody chain", entitiesChecked = 6, matchesFound = 0 }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "MASS_BALANCE", "PASS", checkedAt,
                new { result = "Mass balance verified: input 450kg, output 310kg, loss ratio 31.1% within acceptable range", inputKg = 450, outputKg = 310, lossPercent = 31.1 }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "SEQUENCE_CHECK", "PASS", checkedAt,
                new { result = "Event sequence valid: 6 events in correct chronological order with intact hash chain", eventCount = 6, chainIntact = true })
        );
    }

    private static void SeedComplianceChecks038(AppDbContext db, Guid batchId, Guid tenantId, List<CustodyEventEntity> events, DateTime now)
    {
        // W-2026-038: OECD_DDG = FAIL (DRC high-risk), others PASS
        var lastEvent = events[^1];
        var checkedAt = now.AddDays(-12);

        db.ComplianceChecks.AddRange(
            CreateCheck(lastEvent.Id, batchId, tenantId, "RMAP", "PASS", checkedAt,
                new { result = "No smelter event recorded yet — RMAP check deferred to smelting stage", status = "DEFERRED" }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "OECD_DDG", "FAIL", checkedAt,
                new { result = "OECD DDG check result: FAIL. Sub-checks: origin_risk=FAIL; sanctions=PASS; transport_route=PASS", originCountry = "CD", riskLevel = "HIGH", reason = "DRC (Democratic Republic of the Congo) is classified as CAHRAs under OECD Annex II. Enhanced due diligence required." }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "SANCTIONS", "PASS", checkedAt,
                new { result = "No sanctioned entities found in custody chain", entitiesChecked = 4, matchesFound = 0 }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "MASS_BALANCE", "PASS", checkedAt,
                new { result = "Mass balance verified: input 780kg, output 650kg, loss ratio 16.7% within acceptable range", inputKg = 780, outputKg = 650, lossPercent = 16.7 }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "SEQUENCE_CHECK", "PASS", checkedAt,
                new { result = "Event sequence valid: 4 events in correct chronological order with intact hash chain", eventCount = 4, chainIntact = true })
        );
    }

    private static void SeedComplianceChecks035(AppDbContext db, Guid batchId, Guid tenantId, List<CustodyEventEntity> events, DateTime now)
    {
        // W-2026-035: 4 checks all PASS
        var lastEvent = events[^1];
        var checkedAt = now.AddDays(-20);

        db.ComplianceChecks.AddRange(
            CreateCheck(lastEvent.Id, batchId, tenantId, "RMAP", "PASS", checkedAt,
                new { result = "No smelter event — batch exported to RMAP-conformant facility (CID002158)", smelterId = "CID002158", conformanceStatus = "CONFORMANT" }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "OECD_DDG", "PASS", checkedAt,
                new { result = "OECD DDG check result: PASS. Sub-checks: origin_risk=PASS; sanctions=PASS; transport_route=PASS", originCountry = "RW", riskLevel = "HIGH", mitigated = true }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "MASS_BALANCE", "PASS", checkedAt,
                new { result = "Mass balance verified: input 320kg, output 275kg, loss ratio 14.1% within acceptable range", inputKg = 320, outputKg = 275, lossPercent = 14.1 }),
            CreateCheck(lastEvent.Id, batchId, tenantId, "SEQUENCE_CHECK", "PASS", checkedAt,
                new { result = "Event sequence valid: 5 events in correct chronological order with intact hash chain", eventCount = 5, chainIntact = true })
        );
    }

    private static ComplianceCheckEntity CreateCheck(
        Guid custodyEventId, Guid batchId, Guid tenantId,
        string framework, string status, DateTime checkedAt,
        object details)
    {
        return new ComplianceCheckEntity
        {
            Id = Guid.NewGuid(),
            CustodyEventId = custodyEventId,
            BatchId = batchId,
            TenantId = tenantId,
            Framework = framework,
            Status = status,
            Details = JsonSerializer.SerializeToElement(details),
            CheckedAt = checkedAt,
            RuleVersion = "1.0.0-pilot"
        };
    }
}
